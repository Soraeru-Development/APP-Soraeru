using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrSourceLanguageInferenceTests
{
    [Theory]
    [InlineData("이 근처에 환전소가 어디에 있어요?", "ko")]
    [InlineData("환전소가", "ko")]
    [InlineData("こんにちは", "ja")]
    [InlineData("カタカナ", "ja")]
    [InlineData("日本語の単語", "ja")]
    [InlineData("สวัสดีครับ", "th")]
    [InlineData("phở", "vi")]
    [InlineData("Đà Nẵng", "vi")]
    [InlineData("Я женщина.", "ru")]
    [InlineData("Привет", "ru")]
    [InlineData("España", "es")]
    [InlineData("¿Cómo estás?", "es")]
    [InlineData("¡Hola!", "es")]
    [InlineData("مرحبا", "ar")]
    [InlineData("नमस्ते", "hi")]
    [InlineData("မင်္ဂလာပါ", "my")]
    [InlineData("សួស្តី", "km")]
    [InlineData("ສະບາຍດີ", "lo")]
    public void Infer_maps_distinctive_scripts(string text, string expected) =>
        OcrSourceLanguageInference.Infer(text).ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hello world")]
    [InlineData("Magandang umaga")]
    [InlineData("漢字詞彙")]
    [InlineData("中文词")]
    [InlineData("café")]
    [InlineData("Buenos dias")]
    [InlineData("gracias")]
    public void Infer_returns_auto_when_unreliable(string? text) =>
        OcrSourceLanguageInference.Infer(text).ShouldBe("auto");

    [Fact]
    public void Infer_prefers_majority_when_scripts_mixed_unevenly()
    {
        // Mostly Hangul with a stray kana fragment should still be Korean.
        OcrSourceLanguageInference.Infer("환전소가あ").ShouldBe("ko");
    }

    [Fact]
    public void Infer_returns_auto_on_equal_script_tie()
    {
        OcrSourceLanguageInference.Infer("한あ").ShouldBe("auto");
    }
}
