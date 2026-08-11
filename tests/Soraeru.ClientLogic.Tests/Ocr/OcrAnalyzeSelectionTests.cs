using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrAnalyzeSelectionTests
{
    [Fact]
    public void TryResolve_requires_exactly_one_non_empty_selection()
    {
        OcrAnalyzeSelection.TryResolve(selectedToken: null, out var text, out var error)
            .ShouldBeFalse();
        text.ShouldBeNull();
        error.ShouldBe(OcrAnalyzeSelection.ErrorNothingSelected);

        OcrAnalyzeSelection.TryResolve("   ", out text, out error).ShouldBeFalse();
        error.ShouldBe(OcrAnalyzeSelection.ErrorNothingSelected);
    }

    [Fact]
    public void TryResolve_returns_trimmed_token()
    {
        OcrAnalyzeSelection.TryResolve("  สวัสดี  ", out var text, out var error)
            .ShouldBeTrue();
        text.ShouldBe("สวัสดี");
        error.ShouldBeNull();
    }

    [Fact]
    public void TryResolve_truncates_to_50_chars()
    {
        var longWord = new string('漢', 60);
        OcrAnalyzeSelection.TryResolve(longWord, out var text, out _).ShouldBeTrue();
        text!.Length.ShouldBe(50);
        text.ShouldBe(longWord[..50]);
    }
}
