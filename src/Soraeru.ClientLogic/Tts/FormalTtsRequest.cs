namespace Soraeru.ClientLogic.Tts;

/// <summary>
/// Prepared formal-source utterance for system TTS (never mnemonic / 空耳).
/// </summary>
public sealed record FormalTtsUtterance(
    string SpeechText,
    string LanguageFamily,
    string PreferredLanguageTag);

/// <summary>
/// Resolves what to speak for「播放正式發音」: source text + language locale hints.
/// </summary>
public static class FormalTtsRequest
{
    public const string ErrorEmptySource = "沒有可播放的原文。";

    public static bool TryPrepare(
        string? sourceText,
        string? sourceLanguage,
        out FormalTtsUtterance? utterance,
        out string? error)
    {
        var text = sourceText?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            utterance = null;
            error = ErrorEmptySource;
            return false;
        }

        var family = FormalTtsLocale.NormalizeFamily(sourceLanguage);
        utterance = new FormalTtsUtterance(
            text,
            family,
            FormalTtsLocale.PreferredTag(family));
        error = null;
        return true;
    }
}
