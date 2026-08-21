namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// When notebook language options (excluding「全部」) exceed the chip threshold, UI switches to a picker.
/// </summary>
public static class NotebookLanguageFilterMode
{
    public const int ChipLanguageThreshold = 5;

    public static bool ShouldUsePicker(int languageOptionCountExcludingAll) =>
        languageOptionCountExcludingAll > ChipLanguageThreshold;
}
