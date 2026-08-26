namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Optional post-OCR text assist gate: suggest correction only when quality is suspicious.
/// Never silently overwrites; images must not be uploaded (text-only if a backend call is wired).
/// </summary>
public static class OcrTextAssistGate
{
    public static bool ShouldSuggestAssist(string? ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return false;

        return OcrScriptQuality.LooksLikeCyrillicScriptHallucination(ocrText)
            || OcrScriptQuality.IsSuspiciousLatinOcr(ocrText);
    }

    /// <summary>
    /// Builds an editable suggestion envelope. Proposed text may come from a future thin API;
    /// until then App may leave <paramref name="proposedText"/> null and show a confirm stub.
    /// </summary>
    public static OcrTextAssistSuggestion BuildEditableSuggestionStub(
        string originalText,
        string? proposedText = null,
        string? proposedLanguage = null) =>
        new(
            OriginalText: originalText,
            ProposedText: proposedText,
            ProposedLanguage: proposedLanguage,
            RequiresUserConfirm: true,
            ImagesUploaded: false);
}

public sealed record OcrTextAssistSuggestion(
    string OriginalText,
    string? ProposedText,
    string? ProposedLanguage,
    bool RequiresUserConfirm,
    bool ImagesUploaded);
