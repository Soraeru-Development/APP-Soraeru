namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Pure helpers for 詳情「重新分析」：語言鍵對齊與重產上限前置判斷（票 18／09 銜接）.
/// </summary>
public static class ReanalyzeGuard
{
    public static bool IsRegenerationLimitReached(int remainingRegenerations) =>
        remainingRegenerations <= 0;

    /// <summary>
    /// Resolves analyze／regenerate language key. Never returns <c>auto</c>.
    /// Prefers card <see cref="LocalWordCard.DetectedLanguage"/>, then last flow result language.
    /// </summary>
    public static bool TryResolveSourceLanguage(
        string? cardDetectedLanguage,
        string? lastResultSourceLanguage,
        out string sourceLanguage)
    {
        if (LocalNotebookLookupKey.HasUsableLanguageCode(cardDetectedLanguage))
        {
            sourceLanguage = cardDetectedLanguage!.Trim();
            return true;
        }

        if (LocalNotebookLookupKey.HasUsableLanguageCode(lastResultSourceLanguage))
        {
            sourceLanguage = lastResultSourceLanguage!.Trim();
            return true;
        }

        sourceLanguage = string.Empty;
        return false;
    }

    /// <summary>
    /// Whether <paramref name="flowResultRemainingRegenerations"/> applies to this card
    /// (same normalized text + language key as ticket 09).
    /// </summary>
    public static bool FlowResultMatchesCard(
        string cardNormalizedText,
        string cardDetectedLanguage,
        string flowResultNormalizedText,
        string flowResultSourceLanguage)
    {
        if (!LocalNotebookLookupKey.HasUsableLanguageCode(cardDetectedLanguage))
            return false;

        if (!LocalNotebookLookupKey.HasUsableLanguageCode(flowResultSourceLanguage))
            return false;

        var cardLang = cardDetectedLanguage.Trim().ToLowerInvariant();
        var resultLang = flowResultSourceLanguage.Trim().ToLowerInvariant();
        if (!string.Equals(cardLang, resultLang, StringComparison.Ordinal))
            return false;

        var cardNorm = LocalNotebookLookupKey.NormalizeText(cardNormalizedText);
        var resultNorm = LocalNotebookLookupKey.NormalizeText(flowResultNormalizedText);
        return string.Equals(cardNorm, resultNorm, StringComparison.Ordinal);
    }
}
