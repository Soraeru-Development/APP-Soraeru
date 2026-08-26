using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrImageEnhanceHintsTests
{
    [Theory]
    [InlineData(40, true)]
    [InlineData(109, true)]
    [InlineData(110, false)]
    [InlineData(200, false)]
    public void ShouldInvertForOcr_by_mean_luminance(double mean, bool expected) =>
        OcrImageEnhanceHints.ShouldInvertForOcr(mean).ShouldBe(expected);

    [Theory]
    [InlineData(200, 20, true)]
    [InlineData(200, 47, true)]
    [InlineData(200, 48, false)]
    [InlineData(149, 20, false)]
    [InlineData(100, 10, false)]
    public void ShouldBoostContrastForOcr_bright_low_stddev(
        double mean,
        double stdDev,
        bool expected) =>
        OcrImageEnhanceHints.ShouldBoostContrastForOcr(mean, stdDev).ShouldBe(expected);

    [Theory]
    [InlineData(400, 300, true)]
    [InlineData(899, 500, true)]
    [InlineData(900, 500, false)]
    [InlineData(1200, 800, false)]
    [InlineData(1200, 280, true)]
    [InlineData(1600, 319, true)]
    [InlineData(1600, 320, false)]
    [InlineData(0, 100, false)]
    public void ShouldUpscale_by_long_or_short_edge(int width, int height, bool expected) =>
        OcrImageEnhanceHints.ShouldUpscale(width, height).ShouldBe(expected);

    [Fact]
    public void EmptyResultGuidance_arabic_mentions_screen_capture()
    {
        var msg = OcrImageEnhanceHints.EmptyResultGuidance(OcrScriptFamilyHint.Arabic);
        msg.ShouldContain("螢幕");
        msg.ShouldContain("手動");
    }
}
