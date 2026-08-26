using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrTextAssistGateTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("hola amigo", false)]
    [InlineData("Я женщина.", false)]
    [InlineData("weHUAMHa.", true)]
    [InlineData("xxzq", true)]
    public void ShouldSuggestAssist_only_when_quality_suspicious(string? text, bool expected) =>
        OcrTextAssistGate.ShouldSuggestAssist(text).ShouldBe(expected);

    [Fact]
    public void BuildSuggestion_never_overwrites_original()
    {
        const string original = "weHUAMHa.";
        var suggestion = OcrTextAssistGate.BuildEditableSuggestionStub(original, proposedText: "Я женщина.");
        suggestion.OriginalText.ShouldBe(original);
        suggestion.ProposedText.ShouldBe("Я женщина.");
        suggestion.RequiresUserConfirm.ShouldBeTrue();
        suggestion.ImagesUploaded.ShouldBeFalse();
    }
}
