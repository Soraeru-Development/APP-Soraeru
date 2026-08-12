using System.Text;

namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Short-circuit key helpers (OwnerUserId + DetectedLanguage + NormalizedText). ADR-0008.
/// </summary>
public static class LocalNotebookLookupKey
{
    public static bool HasUsableLanguageCode(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return false;

        var trimmed = language.Trim();
        if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return false;
        if (trimmed.Equals("und", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public static string NormalizeText(string text)
    {
        var collapsed = string.Join(
            ' ',
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return collapsed.Normalize(NormalizationForm.FormC);
    }
}
