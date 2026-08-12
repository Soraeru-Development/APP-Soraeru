using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

/// <summary>
/// Pure navigation / login gate for 本機短路＋重新分析（票 18／ADR-0008）.
/// </summary>
public sealed class AnalyzeEntryGateTests
{
    private static readonly Guid CardId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void DecideLookup_when_local_hit_opens_detail_without_analyze_or_quota()
    {
        var card = SampleCard();
        var decision = AnalyzeEntryGate.DecideLookup(card, isAuthenticated: true);

        decision.Kind.ShouldBe(AnalyzeEntryKind.OpenLocalDetail);
        decision.CardId.ShouldBe(CardId);
        decision.CallsAnalyze.ShouldBeFalse();
        decision.ForceRefresh.ShouldBeFalse();
        decision.CountsTowardQuota.ShouldBeFalse();
    }

    [Fact]
    public void DecideLookup_anonymous_hit_still_opens_detail_without_analyze()
    {
        var decision = AnalyzeEntryGate.DecideLookup(SampleCard(), isAuthenticated: false);

        decision.Kind.ShouldBe(AnalyzeEntryKind.OpenLocalDetail);
        decision.CardId.ShouldBe(CardId);
        decision.CallsAnalyze.ShouldBeFalse();
        decision.CountsTowardQuota.ShouldBeFalse();
    }

    [Fact]
    public void DecideLookup_authenticated_miss_proceeds_to_analyze_counting_quota()
    {
        var decision = AnalyzeEntryGate.DecideLookup(match: null, isAuthenticated: true);

        decision.Kind.ShouldBe(AnalyzeEntryKind.ProceedToAnalyze);
        decision.CardId.ShouldBeNull();
        decision.CallsAnalyze.ShouldBeTrue();
        decision.ForceRefresh.ShouldBeFalse();
        decision.CountsTowardQuota.ShouldBeTrue();
    }

    [Fact]
    public void DecideLookup_anonymous_miss_requires_login_and_does_not_analyze()
    {
        var decision = AnalyzeEntryGate.DecideLookup(match: null, isAuthenticated: false);

        decision.Kind.ShouldBe(AnalyzeEntryKind.RequireLogin);
        decision.CallsAnalyze.ShouldBeFalse();
        decision.CountsTowardQuota.ShouldBeFalse();
    }

    [Fact]
    public void DecideReanalyze_authenticated_forces_refresh_and_counts_quota()
    {
        var decision = AnalyzeEntryGate.DecideReanalyze(isAuthenticated: true);

        decision.Kind.ShouldBe(AnalyzeEntryKind.ProceedToAnalyze);
        decision.CallsAnalyze.ShouldBeTrue();
        decision.ForceRefresh.ShouldBeTrue();
        decision.CountsTowardQuota.ShouldBeTrue();
    }

    [Fact]
    public void DecideReanalyze_anonymous_requires_login()
    {
        var decision = AnalyzeEntryGate.DecideReanalyze(isAuthenticated: false);

        decision.Kind.ShouldBe(AnalyzeEntryKind.RequireLogin);
        decision.CallsAnalyze.ShouldBeFalse();
        decision.ForceRefresh.ShouldBeFalse();
        decision.CountsTowardQuota.ShouldBeFalse();
    }

    private static LocalWordCard SampleCard()
    {
        var now = DateTimeOffset.Parse("2026-08-12T00:00:00Z");
        return new LocalWordCard(
            CardId,
            Owner,
            "ありがとう",
            "ありがとう",
            "ja",
            "謝謝",
            "arigatou",
            "啊哩嘎多",
            now,
            now,
            null);
    }
}
