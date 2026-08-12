using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

/// <summary>
/// 詳情「重新分析」語言鍵與重產上限前置檢查（票 18／09 銜接）.
/// </summary>
public sealed class ReanalyzeGuardTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    public void IsRegenerationLimitReached_when_remaining_at_or_below_zero(int remaining, bool expected)
    {
        ReanalyzeGuard.IsRegenerationLimitReached(remaining).ShouldBe(expected);
    }

    [Fact]
    public void TryResolveSourceLanguage_prefers_card_detected_language()
    {
        var ok = ReanalyzeGuard.TryResolveSourceLanguage("ja", "en", out var lang);

        ok.ShouldBeTrue();
        lang.ShouldBe("ja");
    }

    [Fact]
    public void TryResolveSourceLanguage_falls_back_to_last_result_when_card_missing()
    {
        var ok = ReanalyzeGuard.TryResolveSourceLanguage(null, "th", out var lang);

        ok.ShouldBeTrue();
        lang.ShouldBe("th");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("auto")]
    [InlineData("und")]
    public void TryResolveSourceLanguage_rejects_auto_or_missing(string? cardLanguage)
    {
        var ok = ReanalyzeGuard.TryResolveSourceLanguage(cardLanguage, null, out var lang);

        ok.ShouldBeFalse();
        lang.ShouldBeEmpty();
    }

    [Fact]
    public void FlowResultMatchesCard_when_same_normalized_text_and_language()
    {
        var matches = ReanalyzeGuard.FlowResultMatchesCard(
            "  ありがとう  ",
            "ja",
            "ありがとう",
            "ja");

        matches.ShouldBeTrue();
    }

    [Fact]
    public void FlowResultMatchesCard_false_when_language_differs()
    {
        var matches = ReanalyzeGuard.FlowResultMatchesCard(
            "hello",
            "en",
            "hello",
            "ja");

        matches.ShouldBeFalse();
    }

    [Fact]
    public void FlowResultMatchesCard_false_when_card_language_unusable()
    {
        var matches = ReanalyzeGuard.FlowResultMatchesCard(
            "hello",
            "auto",
            "hello",
            "en");

        matches.ShouldBeFalse();
    }
}
