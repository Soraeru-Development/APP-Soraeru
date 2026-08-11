using NSubstitute;
using Shouldly;
using Soraeru.Application.Abstractions.Auth;
using Soraeru.Application.Abstractions.Llm;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Application.Analyze;
using Soraeru.Application.Common;
using Soraeru.Application.Curator;
using Soraeru.Application.Quota;
using Soraeru.Application.Tests.Analyze;

namespace Soraeru.Application.Tests.Curator;

public sealed class CuratorMnemonicServiceTests
{
    private static readonly Guid CuratorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid LearnerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IVerifiedMnemonicRepository _verified = Substitute.For<IVerifiedMnemonicRepository>();
    private readonly IDeveloperAccountPolicy _policy = Substitute.For<IDeveloperAccountPolicy>();
    private readonly CuratorMnemonicService _sut;

    public CuratorMnemonicServiceTests()
    {
        _sut = new CuratorMnemonicService(_users, _verified, _policy);

        _users.FindByIdAsync(CuratorId, Arg.Any<CancellationToken>())
            .Returns(User(CuratorId, "curator@example.com", isDeveloper: true));
        _users.FindByIdAsync(LearnerId, Arg.Any<CancellationToken>())
            .Returns(User(LearnerId, "learner@example.com", isDeveloper: false));

        _policy.IsDeveloperEmail("curator@example.com").Returns(true);
        _policy.IsDeveloperEmail("learner@example.com").Returns(false);
    }

    [Fact]
    public async Task CreateAsync_non_allowlist_returns_forbidden()
    {
        var result = await _sut.CreateAsync(
            new CreateVerifiedMnemonicCommand(
                LearnerId,
                "en",
                "hello",
                "哈囉核定",
                "ㄏㄚ ㄌㄨㄛˊ",
                "策展提示",
                IsEnabled: true));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("FORBIDDEN");
        await _verified.DidNotReceive()
            .AddAsync(Arg.Any<VerifiedMnemonicRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_allowlist_persists_enabled_entry()
    {
        VerifiedMnemonicRecord? saved = null;
        _verified.AddAsync(Arg.Any<VerifiedMnemonicRecord>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                saved = ci.ArgAt<VerifiedMnemonicRecord>(0);
                return saved;
            });
        _verified.FindByLanguageAndNormalizedAsync("en", "hello", Arg.Any<CancellationToken>())
            .Returns((VerifiedMnemonicRecord?)null);

        var result = await _sut.CreateAsync(
            new CreateVerifiedMnemonicCommand(
                CuratorId,
                "en",
                "hello",
                "哈囉核定",
                "ㄏㄚ ㄌㄨㄛˊ",
                "策展提示",
                IsEnabled: true));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Language.ShouldBe("en");
        result.Value.SourceText.ShouldBe("hello");
        result.Value.NormalizedSource.ShouldBe("hello");
        result.Value.DisplayText.ShouldBe("哈囉核定");
        result.Value.IsEnabled.ShouldBeTrue();
        saved.ShouldNotBeNull();
    }

    [Fact]
    public async Task ListAsync_non_allowlist_returns_forbidden()
    {
        var result = await _sut.ListAsync(LearnerId, language: null, query: null);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("FORBIDDEN");
        await _verified.DidNotReceive()
            .SearchAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetEnabledAsync_disable_then_analyze_misses_verified_path()
    {
        // End-to-end seam inside Application: curator disables → learner analyze misses gold.
        var store = new List<VerifiedMnemonicRecord>();
        var entryId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        store.Add(new VerifiedMnemonicRecord(
            entryId,
            "en",
            "hello",
            "hello",
            "哈囉核定",
            "ㄏㄚ ㄌㄨㄛˊ",
            "策展提示",
            IsEnabled: true,
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-08-01T00:00:00Z")));

        _verified.GetByIdAsync(entryId, Arg.Any<CancellationToken>())
            .Returns(ci => store.FirstOrDefault(e => e.Id == ci.ArgAt<Guid>(0)));
        _verified.UpdateAsync(Arg.Any<VerifiedMnemonicRecord>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var updated = ci.ArgAt<VerifiedMnemonicRecord>(0);
                store.RemoveAll(e => e.Id == updated.Id);
                store.Add(updated);
                return updated;
            });
        _verified.FindActiveByLanguageAndNormalizedAsync("en", "hello", Arg.Any<CancellationToken>())
            .Returns(ci => store.FirstOrDefault(e =>
                e.IsEnabled
                && string.Equals(e.Language, ci.ArgAt<string>(0), StringComparison.OrdinalIgnoreCase)
                && e.NormalizedSource == ci.ArgAt<string>(1)));

        var disable = await _sut.SetEnabledAsync(
            new SetVerifiedMnemonicEnabledCommand(CuratorId, entryId, IsEnabled: false));
        disable.IsSuccess.ShouldBeTrue();
        disable.Value!.IsEnabled.ShouldBeFalse();

        var agent = Substitute.For<IWordAnalysisAgent>();
        var cache = Substitute.For<IAnalysisResultCache>();
        var quota = Substitute.For<IQuotaService>();
        WordAnalysisPayload? cachedOut;
        cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);
        quota.GetRemainingAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>())
            .Returns(new QuotaSnapshot(20, 19, IsUnlimited: false));
        quota.TryConsumeAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>()).Returns(true);
        agent.AnalyzeAsync(Arg.Is<WordAnalysisAgentRequest>(r => !r.SkipMnemonics), Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(
                AnalyzeWordServiceTests.ValidPayload("hello", "哈囉", "嘿囉")));

        var analyze = new AnalyzeWordService(_users, quota, agent, cache, _verified);
        var result = await analyze.AnalyzeAsync(
            new AnalyzeWordCommand(LearnerId, "hello", "en", "zh-TW", "bopomofo"));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.MnemonicSource.ShouldBe(AnalyzeMnemonicSources.LlmDraft);
        await agent.Received(1).AnalyzeAsync(
            Arg.Is<WordAnalysisAgentRequest>(r => !r.SkipMnemonics),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_then_learner_analyze_hits_verified()
    {
        var store = new List<VerifiedMnemonicRecord>();
        _verified.FindByLanguageAndNormalizedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var lang = ci.ArgAt<string>(0);
                var norm = ci.ArgAt<string>(1);
                return store.FirstOrDefault(e =>
                    string.Equals(e.Language, lang, StringComparison.OrdinalIgnoreCase)
                    && e.NormalizedSource == norm);
            });
        _verified.FindActiveByLanguageAndNormalizedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var lang = ci.ArgAt<string>(0);
                var norm = ci.ArgAt<string>(1);
                return store.FirstOrDefault(e =>
                    e.IsEnabled
                    && string.Equals(e.Language, lang, StringComparison.OrdinalIgnoreCase)
                    && e.NormalizedSource == norm);
            });
        _verified.AddAsync(Arg.Any<VerifiedMnemonicRecord>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var entry = ci.ArgAt<VerifiedMnemonicRecord>(0);
                store.Add(entry);
                return entry;
            });

        var created = await _sut.CreateAsync(
            new CreateVerifiedMnemonicCommand(
                CuratorId,
                "en",
                "hello",
                "哈囉核定",
                "ㄏㄚ ㄌㄨㄛˊ",
                "策展提示",
                IsEnabled: true));
        created.IsSuccess.ShouldBeTrue();

        var agent = Substitute.For<IWordAnalysisAgent>();
        var cache = Substitute.For<IAnalysisResultCache>();
        var quota = Substitute.For<IQuotaService>();
        WordAnalysisPayload? cachedOut;
        cache.TryGet(Arg.Any<string>(), out cachedOut).Returns(false);
        quota.GetRemainingAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>())
            .Returns(new QuotaSnapshot(20, 19, IsUnlimited: false));
        quota.TryConsumeAsync(Arg.Any<UserRecord>(), Arg.Any<CancellationToken>()).Returns(true);
        agent.AnalyzeAsync(Arg.Is<WordAnalysisAgentRequest>(r => r.SkipMnemonics), Arg.Any<CancellationToken>())
            .Returns(new WordAnalysisAgentSuccess(new WordAnalysisPayload(
                "hello",
                "hello",
                "en",
                "英語",
                "你好",
                "həˈləʊ",
                Array.Empty<WordAnalysisMnemonic>(),
                "notice")));

        var analyze = new AnalyzeWordService(_users, quota, agent, cache, _verified);
        var result = await analyze.AnalyzeAsync(
            new AnalyzeWordCommand(LearnerId, "hello", "en", "zh-TW", "bopomofo"));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.MnemonicSource.ShouldBe(AnalyzeMnemonicSources.Verified);
        result.Value.Mnemonics[0].DisplayText.ShouldBe("哈囉核定");
        await agent.DidNotReceive().AnalyzeAsync(
            Arg.Is<WordAnalysisAgentRequest>(r => !r.SkipMnemonics),
            Arg.Any<CancellationToken>());
    }

    private static UserRecord User(Guid id, string email, bool isDeveloper) =>
        new(
            id,
            email,
            "hash",
            null,
            "User",
            AppConstants.PlanTierFree,
            AppConstants.FreeDailyQuota,
            "bopomofo",
            isDeveloper,
            OnboardingCompleted: true,
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
}
