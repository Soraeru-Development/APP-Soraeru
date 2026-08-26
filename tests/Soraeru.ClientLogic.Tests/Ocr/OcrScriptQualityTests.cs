using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrScriptQualityTests
{
    [Theory]
    [InlineData("weHUAMHa.", true)]
    [InlineData("weHUAMHa", true)]
    [InlineData("McDonald", false)]
    [InlineData("hola", false)]
    [InlineData("Я женщина.", false)]
    [InlineData("Hello World", false)]
    public void LooksLikeCyrillicScriptHallucination(string text, bool expected) =>
        OcrScriptQuality.LooksLikeCyrillicScriptHallucination(text).ShouldBe(expected);

    [Theory]
    [InlineData("Я женщина.", true)]
    [InlineData("Привет", true)]
    [InlineData("hola", false)]
    [InlineData("weHUAMHa.", false)]
    public void ContainsCyrillic(string text, bool expected) =>
        OcrScriptQuality.ContainsCyrillic(text).ShouldBe(expected);

    [Theory]
    [InlineData("weHUAMHa.", true)]
    [InlineData("xxzq", true)]
    [InlineData("gracias", false)]
    [InlineData("ありがとう", false)]
    public void IsSuspiciousLatinOcr(string text, bool expected) =>
        OcrScriptQuality.IsSuspiciousLatinOcr(text).ShouldBe(expected);
}
