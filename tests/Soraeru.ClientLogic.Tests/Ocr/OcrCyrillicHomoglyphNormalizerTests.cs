using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrCyrillicHomoglyphNormalizerTests
{
    [Fact]
    public void TryRemapPureLookalikeToken_maps_ect_to_est()
    {
        OcrCyrillicHomoglyphNormalizer.TryRemapPureLookalikeToken("ect", out var remapped)
            .ShouldBeTrue();
        remapped.ShouldBe("ест");
    }

    [Fact]
    public void TryRemapPureLookalikeToken_maps_ili_to_ili_cyrillic()
    {
        OcrCyrillicHomoglyphNormalizer.TryRemapPureLookalikeToken("ili", out var remapped)
            .ShouldBeTrue();
        remapped.ShouldBe("или");
    }

    [Fact]
    public void TryRemapPureLookalikeToken_rejects_english_stopword_the()
    {
        OcrCyrillicHomoglyphNormalizer.TryRemapPureLookalikeToken("the", out _)
            .ShouldBeFalse();
    }

    [Fact]
    public void TryRemapPureLookalikeToken_rejects_unmappable_latin()
    {
        OcrCyrillicHomoglyphNormalizer.TryRemapPureLookalikeToken("girl", out _)
            .ShouldBeFalse();
    }

    [Fact]
    public void NormalizeMixedScript_rewrites_latin_lookalike_between_cyrillic()
    {
        var text = OcrCyrillicHomoglyphNormalizer.NormalizeMixedScript("Девочка ect яблоко");
        text.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void NormalizeMixedScript_rewrites_ili_between_cyrillic()
    {
        var text = OcrCyrillicHomoglyphNormalizer.NormalizeMixedScript("Украина ili Бразилия");
        text.ShouldBe("Украина или Бразилия");
    }

    [Fact]
    public void NormalizeMixedScript_leaves_pure_latin_alone()
    {
        var text = OcrCyrillicHomoglyphNormalizer.NormalizeMixedScript("hello ect world");
        text.ShouldBe("hello ect world");
    }

    [Fact]
    public void MergeMissingLookalikeTokens_inserts_ect_between_cyrillic_words()
    {
        var merged = OcrCyrillicHomoglyphNormalizer.MergeMissingLookalikeTokens(
            "Девочка яблоко",
            "Dev ect yabloko");

        merged.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void MergeMissingLookalikeTokens_inserts_ili_from_latin_secondary()
    {
        var merged = OcrCyrillicHomoglyphNormalizer.MergeMissingLookalikeTokens(
            "Украина Бразилия",
            "Ukraina ili Braziliya");

        merged.ShouldBe("Украина или Бразилия");
    }

    [Fact]
    public void MergeMissingLookalikeTokens_inserts_pure_cyrillic_short_from_secondary()
    {
        var merged = OcrCyrillicHomoglyphNormalizer.MergeMissingLookalikeTokens(
            "Девочка яблоко",
            "Девочка ест яблоко");

        merged.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void MergeMissingLookalikeTokens_inserts_pure_cyrillic_ili()
    {
        var merged = OcrCyrillicHomoglyphNormalizer.MergeMissingLookalikeTokens(
            "Украина Бразилия",
            "Украина или Бразилия");

        merged.ShouldBe("Украина или Бразилия");
    }

    [Fact]
    public void TryNormalizeSecondaryShortToken_accepts_pure_cyrillic_est()
    {
        OcrCyrillicHomoglyphNormalizer.TryNormalizeSecondaryShortToken("ест", out var n)
            .ShouldBeTrue();
        n.ShouldBe("ест");
    }

    [Fact]
    public void PreferRicherCyrillic_picks_candidate_with_more_tokens()
    {
        var preferred = OcrCyrillicHomoglyphNormalizer.PreferRicherCyrillic(
            "Девочка яблоко",
            "Девочка ест яблоко");

        preferred.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void MergeMissingLookalikeTokens_inserts_lone_secondary_est_in_middle()
    {
        // Strip / SingleWord OCR often returns only the middle chip.
        var merged = OcrCyrillicHomoglyphNormalizer.MergeMissingLookalikeTokens(
            "Девочка яблоко",
            "ест");

        merged.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void MergeMissingLookalikeTokens_inserts_lone_secondary_ect_in_middle()
    {
        var merged = OcrCyrillicHomoglyphNormalizer.MergeMissingLookalikeTokens(
            "Девочка яблоко",
            "ect");

        merged.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void MergeMissingLookalikeTokens_uses_left_right_anchors_for_ect()
    {
        var merged = OcrCyrillicHomoglyphNormalizer.MergeMissingLookalikeTokens(
            "Девочка яблоко",
            "Девочка ect яблоко");

        merged.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void UnionMissingLookalikeTokens_merges_across_multiple_secondaries()
    {
        var merged = OcrCyrillicHomoglyphNormalizer.UnionMissingLookalikeTokens(
            "Девочка яблоко",
            "Dev yabloko",
            "ect");

        merged.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void MergeMissingLookalikeTokens_anchor_aligns_latinized_left_right()
    {
        // Prefix fold: "Devochka"↔"Девочка", "yabloko"↔"яблоко"
        var merged = OcrCyrillicHomoglyphNormalizer.MergeMissingLookalikeTokens(
            "Девочка яблоко",
            "Devochka ect yabloko");

        merged.ShouldBe("Девочка ест яблоко");
    }

    [Fact]
    public void ReconcileButtonRowMiddle_prefers_mlkit_ect_over_toko()
    {
        var fixedText = OcrCyrillicHomoglyphNormalizer.ReconcileButtonRowMiddle(
            "Девочка токо яблоко",
            "ect");

        AssertLongShortLongMiddle(fixedText, "ест");
    }

    [Fact]
    public void ReconcileButtonRowMiddle_prefers_secondary_est_over_toko()
    {
        var fixedText = OcrCyrillicHomoglyphNormalizer.ReconcileButtonRowMiddle(
            "Девочка токо яблоко",
            "ест");

        AssertLongShortLongMiddle(fixedText, "ест");
    }

    [Fact]
    public void UnionMissingLookalikeTokens_replaces_toko_when_mlkit_has_ect()
    {
        var merged = OcrCyrillicHomoglyphNormalizer.UnionMissingLookalikeTokens(
            "Девочка токо яблоко",
            "Dev ect yabloko");

        AssertLongShortLongMiddle(merged, "ест");
    }

    [Fact]
    public void PreferBestShortToken_prefers_ect_remap_over_toko()
    {
        var best = OcrCyrillicHomoglyphNormalizer.PreferBestShortToken("токо", "ect", "токе");
        best.ShouldBe("ест");
    }

    [Fact]
    public void PreferBestShortToken_prefers_est_over_oko()
    {
        var best = OcrCyrillicHomoglyphNormalizer.PreferBestShortToken("ест", "OKO");
        best.ShouldBe("ест");
    }

    [Fact]
    public void PreferBestShortToken_prefers_est_over_longer_garbage()
    {
        var best = OcrCyrillicHomoglyphNormalizer.PreferBestShortToken("токо", "ест");
        best.ShouldBe("ест");
    }

    [Fact]
    public void PreferBestShortToken_leaves_oko_when_no_better_short()
    {
        var best = OcrCyrillicHomoglyphNormalizer.PreferBestShortToken("ОКО");
        best.ShouldBe("ОКО");
    }

    [Fact]
    public void PreferRicherCyrillic_prefers_est_middle_over_toko_on_token_tie()
    {
        var preferred = OcrCyrillicHomoglyphNormalizer.PreferRicherCyrillic(
            "Девочка токо яблоко",
            "Девочка ест яблоко");

        AssertLongShortLongMiddle(preferred, "ест");
    }

    [Fact]
    public void ScoreMiddleShortCandidate_ranks_lookalike_above_toko()
    {
        var ect = OcrCyrillicHomoglyphNormalizer.ScoreMiddleShortCandidate("ест", fromLatinLookalike: true);
        var toko = OcrCyrillicHomoglyphNormalizer.ScoreMiddleShortCandidate("токо", fromLatinLookalike: false);
        ect.ShouldBeGreaterThan(toko);
    }

    [Fact]
    public void ReconcileButtonRowMiddle_leaves_non_button_row_alone()
    {
        var text = OcrCyrillicHomoglyphNormalizer.ReconcileButtonRowMiddle("токо");
        text.ShouldBe("токо");
    }

    [Theory]
    [InlineData("Девочка токо яблоко", "токо")]
    [InlineData("Девочка ОКО яблоко", "ОКО")]
    public void ReconcileButtonRowMiddle_does_not_invent_est_without_better_candidate(
        string primary,
        string expectedMiddle)
    {
        var text = OcrCyrillicHomoglyphNormalizer.ReconcileButtonRowMiddle(primary);
        AssertLongShortLongMiddle(text, expectedMiddle);
    }

    [Fact]
    public void ReconcileButtonRowMiddle_remaps_latin_oko_but_does_not_invent_est()
    {
        var text = OcrCyrillicHomoglyphNormalizer.ReconcileButtonRowMiddle("Девочка OKO яблоко");
        AssertLongShortLongMiddle(text, "ОКО");
    }

    [Theory]
    [InlineData("Девочка ОКО яблоко", "ect")]
    [InlineData("Девочка ОКО яблоко", "ест")]
    [InlineData("Девочка OKO яблоко", "ect")]
    [InlineData("Девочка OKO яблоко", "ест")]
    [InlineData("Девочка токо яблоко", "ect")]
    public void UnionMissingLookalikeTokens_any_pass_with_est_lookalike_wins_middle(
        string primary,
        string secondary)
    {
        var merged = OcrCyrillicHomoglyphNormalizer.UnionMissingLookalikeTokens(primary, secondary);
        AssertLongShortLongMiddle(merged, "ест");
    }

    [Fact]
    public void PreferBestShortToken_est_lookalike_beats_allcaps_oko()
    {
        var best = OcrCyrillicHomoglyphNormalizer.PreferBestShortToken("ОКО", "OKO", "ect", "токо");
        best.ShouldBe("ест");
    }

    [Fact]
    public void PreferRicherCyrillic_does_not_let_oko_garbage_beat_est()
    {
        var preferred = OcrCyrillicHomoglyphNormalizer.PreferRicherCyrillic(
            "Девочка ест яблоко",
            "Девочка ОКО яблоко");

        AssertLongShortLongMiddle(preferred, "ест");
    }

    [Fact]
    public void PreferRicherCyrillic_does_not_prefer_oko_row_over_two_long_tokens()
    {
        var preferred = OcrCyrillicHomoglyphNormalizer.PreferRicherCyrillic(
            "Девочка яблоко",
            "Девочка ОКО яблоко");

        var tokens = SplitTokens(preferred);
        tokens.ShouldNotContain(t => t.Equals("ОКО", StringComparison.OrdinalIgnoreCase));
        tokens.ShouldNotContain(t => t.Equals("OKO", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScoreMiddleShortCandidate_lowercase_est_outranks_allcaps_oko()
    {
        var est = OcrCyrillicHomoglyphNormalizer.ScoreMiddleShortCandidate("ест", fromLatinLookalike: false);
        var oko = OcrCyrillicHomoglyphNormalizer.ScoreMiddleShortCandidate("ОКО", fromLatinLookalike: true);
        est.ShouldBeGreaterThan(oko);
    }

    [Theory]
    [InlineData("ОКО")]
    [InlineData("OKO")]
    [InlineData("OкO")]
    public void IsHighConfusionMiddleGarbage_matches_allcaps_or_mixed(string token)
    {
        OcrCyrillicHomoglyphNormalizer.IsHighConfusionMiddleGarbage(token).ShouldBeTrue();
    }

    [Theory]
    [InlineData("ест")]
    [InlineData("или")]
    [InlineData("ect")]
    public void IsHighConfusionMiddleGarbage_spares_preferred_shorts(string token)
    {
        OcrCyrillicHomoglyphNormalizer.IsHighConfusionMiddleGarbage(token).ShouldBeFalse();
    }

    [Fact]
    public void ReconcileButtonRowMiddle_does_not_rewrite_unrelated_allcaps_net()
    {
        var text = OcrCyrillicHomoglyphNormalizer.ReconcileButtonRowMiddle("Девочка НЕТ яблоко");
        AssertLongShortLongMiddle(text, "НЕТ");
    }

    [Fact]
    public void ReconcileButtonRowMiddle_leaves_ili_middle_alone()
    {
        var text = OcrCyrillicHomoglyphNormalizer.ReconcileButtonRowMiddle("Украина или Бразилия");
        AssertLongShortLongMiddle(text, "или");
    }

    static void AssertLongShortLongMiddle(string text, string expectedMiddle)
    {
        var tokens = SplitTokens(text);
        tokens.Count.ShouldBe(3);
        CountLetters(tokens[0]).ShouldBeGreaterThanOrEqualTo(
            OcrCyrillicHomoglyphNormalizer.MinSideTokenLengthForButtonRow);
        CountLetters(tokens[2]).ShouldBeGreaterThanOrEqualTo(
            OcrCyrillicHomoglyphNormalizer.MinSideTokenLengthForButtonRow);
        tokens[1].ShouldBe(expectedMiddle);
    }

    static List<string> SplitTokens(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    static int CountLetters(string token)
    {
        var n = 0;
        foreach (var rune in token.EnumerateRunes())
        {
            if (OcrScriptQuality.IsLatinLetter(rune.Value) || OcrScriptQuality.IsCyrillicScript(rune.Value))
                n++;
        }

        return n;
    }
}
