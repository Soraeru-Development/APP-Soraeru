using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrTextTokenizerTests
{
    [Fact]
    public void Tokenize_splits_whitespace_separated_words_preserving_order()
    {
        var tokens = OcrTextTokenizer.Tokenize("hello  world\nไทย");

        tokens.ShouldBe(["hello", "world", "ไทย"]);
    }

    [Fact]
    public void Tokenize_empty_or_whitespace_returns_empty()
    {
        OcrTextTokenizer.Tokenize(null).ShouldBeEmpty();
        OcrTextTokenizer.Tokenize("   \n\t  ").ShouldBeEmpty();
    }

    [Fact]
    public void Tokenize_dedupes_exact_duplicates_keeping_first()
    {
        var tokens = OcrTextTokenizer.Tokenize("abc xyz abc");

        tokens.ShouldBe(["abc", "xyz"]);
    }

    [Fact]
    public void Tokenize_truncates_tokens_longer_than_50_chars()
    {
        var longWord = new string('あ', 60);
        var tokens = OcrTextTokenizer.Tokenize(longWord);

        tokens.Count.ShouldBe(1);
        tokens[0].Length.ShouldBe(50);
        tokens[0].ShouldBe(longWord[..50]);
    }

    [Fact]
    public void Tokenize_unspaced_cjk_run_becomes_single_candidate()
    {
        var tokens = OcrTextTokenizer.Tokenize("日本語の単語");

        tokens.ShouldBe(["日本語の単語"]);
    }

    [Fact]
    public void Tokenize_filters_icon_noise_beside_cyrillic()
    {
        var tokens = OcrTextTokenizer.Tokenize("$ ) Я женщина. PP\" Ч");

        tokens.ShouldBe(["Я", "женщина."]);
    }

    [Fact]
    public void StripNoiseTokens_removes_junk_keeps_cyrillic_words()
    {
        var cleaned = OcrTextTokenizer.StripNoiseTokens("$ ) Я женщина.\nPP\" Ч");

        cleaned.ShouldBe("Я женщина.");
    }

    [Fact]
    public void Tokenize_keeps_latin_when_no_cyrillic_context()
    {
        var tokens = OcrTextTokenizer.Tokenize("hello PP world");

        tokens.ShouldBe(["hello", "PP", "world"]);
    }

    [Fact]
    public void IsLikelyVocabularyToken_rejects_symbol_only()
    {
        OcrTextTokenizer.IsLikelyVocabularyToken("$)", cyrillicContext: true).ShouldBeFalse();
        OcrTextTokenizer.IsLikelyVocabularyToken("PP\"", cyrillicContext: true).ShouldBeFalse();
    }
}
