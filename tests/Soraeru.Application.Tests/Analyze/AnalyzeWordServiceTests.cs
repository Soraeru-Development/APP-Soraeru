using NSubstitute;
using Shouldly;
using Soraeru.Application.Abstractions.Llm;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Application.Analyze;
using Soraeru.Application.Common;
using Soraeru.Application.Quota;

namespace Soraeru.Application.Tests.Analyze;

/// <summary>
/// Prefactor anchor + hard-gate / draft-badge behaviour for AnalyzeWordService.
/// </summary>
public sealed class AnalyzeWordServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IQuotaService _quota = Substitute.For<IQuotaService>();
    private readonly IWordAnalysisAgent _agent = Substitute.For<IWordAnalysisAgent>();
    private readonly IAnalysisResultCache _cache = Substitute.For<IAnalysisResultCache>();
    private readonly IVerifiedMnemonicRepository _verified = Substitute.For<IVerifiedMnemonicRepository>();
    private readonly IWordRegenerationRepository _regenerations = Substitute.For<IWordRegenerationRepository>();
    private readonly AnalyzeWordService _sut;

    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AnalyzeWordServiceTests()
    {
        _sut = new AnalyzeWordService(_users, _quota, _agent, _cache, _verified, _regenerations);
        _verified.FindActiveByLanguageAndNormalizedAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((VerifiedMnemonicRecord?)null);
        _regenerations.GetCountAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(0);
    }

    [Fact]
    public async Task AnalyzeAsync_force_refresh_when_regeneration_cap_reached_returns_clear_error()
    {
        ArrangeHappyUserAndQuota();
        WordAnalysisPayload? cachedOut;
        _cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);
        _regenerations.GetCountAsync(UserId, "en", "hello", Arg.Any<CancellationToken>())
            .Returns(AnalyzeWordService.MaxRegenerationsPerWord);

        var result = await _sut.AnalyzeAsync(
            new AnalyzeWordCommand(UserId, "hello", "en", "zh-TW", "bopomofo", ForceRefresh: true));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(AnalyzeWordService.RegenerationLimitErrorCode);
        result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        result.Value.ShouldBeNull();
        await _agent.DidNotReceive().AnalyzeAsync(Arg.Any<WordAnalysisAgentRequest>(), Arg.Any<CancellationToken>());
        await _quota.DidNotReceive().TryConsumeAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_successful_force_refresh_increments_regeneration_and_consumes_quota()
    {
        ArrangeHappyUserAndQuota();
        WordAnalysisPayload? cachedOut;
        _cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);
        _regenerations.GetCountAsync(UserId, "en", "hello", Arg.Any<CancellationToken>())
            .Returns(1);

        _agent.AnalyzeAsync(Arg.Any<WordAnalysisAgentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(ValidPayload("hello", "哈囉", "嘿囉")));

        var result = await _sut.AnalyzeAsync(
            new AnalyzeWordCommand(UserId, "hello", "en", "zh-TW", "bopomofo", ForceRefresh: true));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.RemainingRegenerations.ShouldBe(AnalyzeWordService.MaxRegenerationsPerWord - 2);
        await _quota.Received(1).TryConsumeAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>());
        await _regenerations.Received(1).IncrementAsync(UserId, "en", "hello", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_non_force_refresh_does_not_increment_regeneration_count()
    {
        ArrangeHappyUserAndQuota();
        WordAnalysisPayload? cachedOut;
        _cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);

        _agent.AnalyzeAsync(Arg.Any<WordAnalysisAgentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(ValidPayload("hello", "哈囉", "嘿囉")));

        var result = await _sut.AnalyzeAsync(
            new AnalyzeWordCommand(UserId, "hello", "en", "zh-TW", "bopomofo", ForceRefresh: false));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RemainingRegenerations.ShouldBe(AnalyzeWordService.MaxRegenerationsPerWord);
        await _quota.Received(1).TryConsumeAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>());
        await _regenerations.DidNotReceive().IncrementAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_with_fake_agent_returns_success_without_real_llm()
    {
        ArrangeHappyUserAndQuota();
        WordAnalysisPayload? cachedOut;
        _cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);

        _agent.AnalyzeAsync(Arg.Any<WordAnalysisAgentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(ValidPayload("hello", "哈囉", "嘿囉")));

        var result = await _sut.AnalyzeAsync(
            new AnalyzeWordCommand(UserId, "hello", "en", "zh-TW", "bopomofo"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.SourceText.ShouldBe("hello");
        result.Value.Meaning.ShouldBe("你好");
        result.Value.Mnemonics.Count.ShouldBe(2);
        result.Value.Mnemonics[0].DisplayText.ShouldBe("哈囉");
        result.Value.MnemonicSource.ShouldBe(AnalyzeMnemonicSources.LlmDraft);
        result.Value.RemainingDailyQuota.ShouldBe(19);
        await _agent.Received(1).AnalyzeAsync(Arg.Any<WordAnalysisAgentRequest>(), Arg.Any<CancellationToken>());
        await _quota.Received(1).TryConsumeAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_verified_hit_uses_curated_mnemonics_skips_empty_ear_llm_and_marks_verified()
    {
        ArrangeHappyUserAndQuota();
        WordAnalysisPayload? cachedOut;
        _cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);

        var entryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        _verified.FindActiveByLanguageAndNormalizedAsync("en", "hello", Arg.Any<CancellationToken>())
            .Returns(new VerifiedMnemonicRecord(
                entryId,
                "en",
                "hello",
                "hello",
                "哈囉核定",
                "ㄏㄚ ㄌㄨㄛˊ",
                "策展聽感提示",
                IsEnabled: true,
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-08-01T00:00:00Z")));

        // Meaning/reading-only agent reply — mnemonics from LLM must be ignored if present.
        _agent.AnalyzeAsync(
                Arg.Is<WordAnalysisAgentRequest>(r => r.SkipMnemonics),
                Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(new WordAnalysisPayload(
                "hello",
                "hello",
                "en",
                "英語",
                "你好（詞義）",
                "həˈləʊ",
                Array.Empty<WordAnalysisMnemonic>(),
                "近似音僅供記憶")));

        var result = await _sut.AnalyzeAsync(
            new AnalyzeWordCommand(UserId, "hello", "en", "zh-TW", "bopomofo"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.MnemonicSource.ShouldBe(AnalyzeMnemonicSources.Verified);
        result.Value.Meaning.ShouldBe("你好（詞義）");
        result.Value.ReadingText.ShouldBe("həˈləʊ");
        result.Value.Mnemonics.Count.ShouldBe(1);
        result.Value.Mnemonics[0].DisplayText.ShouldBe("哈囉核定");
        result.Value.Mnemonics[0].NotationText.ShouldBe("ㄏㄚ ㄌㄨㄛˊ");
        result.Value.Mnemonics[0].Explanation.ShouldBe("策展聽感提示");

        await _agent.Received(1).AnalyzeAsync(
            Arg.Is<WordAnalysisAgentRequest>(r => r.SkipMnemonics),
            Arg.Any<CancellationToken>());
        await _agent.DidNotReceive().AnalyzeAsync(
            Arg.Is<WordAnalysisAgentRequest>(r => !r.SkipMnemonics),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_verified_hit_trusts_curated_even_if_hard_gate_would_fail()
    {
        ArrangeHappyUserAndQuota();
        WordAnalysisPayload? cachedOut;
        _cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);

        // Curated displayText would fail MnemonicHardGate (latin syllable residue) — still trust.
        _verified.FindActiveByLanguageAndNormalizedAsync("en", "hello", Arg.Any<CancellationToken>())
            .Returns(new VerifiedMnemonicRecord(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                "en",
                "hello",
                "hello",
                "瓦兒dei",
                "ㄨㄚˇ ㄦ",
                "策展刻意保留",
                IsEnabled: true,
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-08-01T00:00:00Z")));

        _agent.AnalyzeAsync(
                Arg.Is<WordAnalysisAgentRequest>(r => r.SkipMnemonics),
                Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(new WordAnalysisPayload(
                "hello",
                "hello",
                "en",
                "英語",
                "你好",
                "həˈləʊ",
                Array.Empty<WordAnalysisMnemonic>(),
                "notice")));

        var result = await _sut.AnalyzeAsync(
            new AnalyzeWordCommand(UserId, "hello", "en", "zh-TW", "bopomofo"));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.MnemonicSource.ShouldBe(AnalyzeMnemonicSources.Verified);
        result.Value.Mnemonics[0].DisplayText.ShouldBe("瓦兒dei");
        await _agent.DidNotReceive().AnalyzeAsync(
            Arg.Is<WordAnalysisAgentRequest>(r => !r.SkipMnemonics),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_verified_hit_only_affects_result_candidates_and_does_not_depend_on_word_cards()
    {
        // 票 17：金標優先只進分析結果候選；AnalyzeWordService 不得依賴／回寫 WordCards。
        ArrangeHappyUserAndQuota();
        WordAnalysisPayload? cachedOut;
        _cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);

        _verified.FindActiveByLanguageAndNormalizedAsync("en", "hello", Arg.Any<CancellationToken>())
            .Returns(new VerifiedMnemonicRecord(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                "en",
                "hello",
                "hello",
                "哈囉核定",
                "ㄏㄚ ㄌㄨㄛˊ",
                "策展",
                IsEnabled: true,
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-08-01T00:00:00Z")));

        _agent.AnalyzeAsync(
                Arg.Is<WordAnalysisAgentRequest>(r => r.SkipMnemonics),
                Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(new WordAnalysisPayload(
                "hello",
                "hello",
                "en",
                "英語",
                "你好",
                "həˈləʊ",
                Array.Empty<WordAnalysisMnemonic>(),
                "notice")));

        var result = await _sut.AnalyzeAsync(
            new AnalyzeWordCommand(UserId, "hello", "en", "zh-TW", "bopomofo"));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.MnemonicSource.ShouldBe(AnalyzeMnemonicSources.Verified);
        result.Value.Mnemonics.Count.ShouldBe(1);
        result.Value.Mnemonics[0].DisplayText.ShouldBe("哈囉核定");

        var ctorParams = typeof(AnalyzeWordService).GetConstructors().Single().GetParameters();
        ctorParams.ShouldNotContain(p =>
            p.ParameterType == typeof(IWordCardRepository)
            || p.ParameterType.Name.Contains("WordCard", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnalyzeAsync_hard_gate_failure_exhausted_returns_clear_error_without_success_candidates()
    {
        ArrangeHappyUserAndQuota();
        WordAnalysisPayload? cachedOut;
        _cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);

        // Schema-valid but hard-gate-illegal mnemonics on every attempt.
        _agent.AnalyzeAsync(Arg.Any<WordAnalysisAgentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(ValidPayload("hello", "瓦兒", "dei")));

        var result = await _sut.AnalyzeAsync(
            new AnalyzeWordCommand(UserId, "hello", "en", "zh-TW", "bopomofo", ForceRefresh: true));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(MnemonicHardGate.FailureCode);
        result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        result.Value.ShouldBeNull();
        await _agent.Received(2).AnalyzeAsync(Arg.Any<WordAnalysisAgentRequest>(), Arg.Any<CancellationToken>());
        await _quota.DidNotReceive().TryConsumeAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>());
    }

    private void ArrangeHappyUserAndQuota()
    {
        var user = new UserRecord(
            UserId,
            "learner@example.com",
            "hash",
            null,
            "Learner",
            AppConstants.PlanTierFree,
            AppConstants.FreeDailyQuota,
            "bopomofo",
            IsDeveloper: false,
            OnboardingCompleted: true,
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        _users.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _quota.GetRemainingAsync(user, Arg.Any<CancellationToken>())
            .Returns(new QuotaSnapshot(20, 19, IsUnlimited: false));
        _quota.TryConsumeAsync(user, Arg.Any<CancellationToken>()).Returns(true);
    }

    internal static WordAnalysisPayload ValidPayload(
        string source,
        string mnemonic1,
        string mnemonic2) =>
        new(
            source,
            source,
            "en",
            "英語",
            "你好",
            "həˈləʊ",
            new[]
            {
                new WordAnalysisMnemonic(mnemonic1, "bopomofo", "ㄏㄚ ㄌㄨㄛˊ", "提示一"),
                new WordAnalysisMnemonic(mnemonic2, "bopomofo", "ㄏㄟ ㄌㄨㄛˊ", "提示二")
            },
            "近似音僅供記憶");
}
