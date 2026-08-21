using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class NotebookLanguageFilterModeTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(12, true)]
    public void ShouldUsePicker_when_language_options_exceed_five(
        int languageCountExcludingAll,
        bool expectedPicker)
    {
        NotebookLanguageFilterMode.ShouldUsePicker(languageCountExcludingAll)
            .ShouldBe(expectedPicker);
    }
}
