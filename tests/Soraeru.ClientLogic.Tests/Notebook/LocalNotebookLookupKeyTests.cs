using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class LocalNotebookLookupKeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("auto")]
    [InlineData("AUTO")]
    [InlineData("und")]
    [InlineData("UND")]
    public void HasUsableLanguageCode_rejects_unknown_or_auto(string? language)
    {
        LocalNotebookLookupKey.HasUsableLanguageCode(language).ShouldBeFalse();
    }

    [Theory]
    [InlineData("ja")]
    [InlineData("en")]
    [InlineData(" th ")]
    [InlineData("ko")]
    public void HasUsableLanguageCode_accepts_concrete_codes(string language)
    {
        LocalNotebookLookupKey.HasUsableLanguageCode(language).ShouldBeTrue();
    }

    [Fact]
    public void NormalizeText_collapses_whitespace_and_nfc()
    {
        LocalNotebookLookupKey.NormalizeText("  hello   world  ").ShouldBe("hello world");
    }
}
