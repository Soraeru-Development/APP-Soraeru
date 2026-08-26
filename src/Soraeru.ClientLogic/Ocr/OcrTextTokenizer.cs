namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Splits on-device OCR full text into selectable word/phrase candidates (single-select UI).
/// Drops icon / UI noise tokens (e.g. "$)", "PP\"") that are not vocabulary-like.
/// </summary>
public static class OcrTextTokenizer
{
    public const int MaxTokenLength = 50;

    public static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var parts = text.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var cyrillicContext = OcrScriptQuality.ContainsCyrillic(text);
        var result = new List<string>(parts.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var part in parts)
        {
            var token = Truncate(part);
            if (token.Length == 0)
                continue;
            if (!IsLikelyVocabularyToken(token, cyrillicContext))
                continue;
            if (!seen.Add(token))
                continue;
            result.Add(token);
        }

        return result;
    }

    /// <summary>
    /// Rebuilds whitespace-separated text without icon/UI noise tokens (keeps order, no dedupe).
    /// </summary>
    public static string StripNoiseTokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        var parts = text.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return string.Empty;

        var cyrillicContext = OcrScriptQuality.ContainsCyrillic(text);
        var kept = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            var token = Truncate(part);
            if (token.Length == 0)
                continue;
            if (!IsLikelyVocabularyToken(token, cyrillicContext))
                continue;
            kept.Add(token);
        }

        return kept.Count == 0 ? string.Empty : string.Join(' ', kept);
    }

    /// <summary>
    /// True when the token looks like a learnable word rather than OCR junk from icons/borders.
    /// </summary>
    public static bool IsLikelyVocabularyToken(string token, bool cyrillicContext = false)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var letters = 0;
        var junk = 0;
        var latinLetters = 0;
        var cyrillicLetters = 0;

        foreach (var rune in token.EnumerateRunes())
        {
            var v = rune.Value;
            if (IsLetterAnyScript(v))
            {
                letters++;
                if (OcrScriptQuality.IsLatinLetter(v))
                    latinLetters++;
                if (OcrScriptQuality.IsCyrillicScript(v))
                    cyrillicLetters++;
                continue;
            }

            if (v is '-' or '\'' or '\u2019' or '.')
                continue;

            junk++;
        }

        if (letters == 0)
            return false;

        // More symbol/digit junk than letters → icon debris ("$)", "PP\"").
        if (junk >= letters)
            return false;

        if (!cyrillicContext || cyrillicLetters > 0)
        {
            // Single Cyrillic glyph from UI debris (e.g. "Ч") — keep only real 1-letter words.
            if (cyrillicContext
                && letters == 1
                && cyrillicLetters == 1
                && junk == 0
                && !IsAllowedSingleCyrillicWord(token))
            {
                return false;
            }

            return true;
        }

        // Beside Cyrillic: drop Latin-only crumbs from icons / borders.
        if (latinLetters > 0 && latinLetters <= 3 && junk > 0)
            return false;

        if (latinLetters == 1 && junk == 0)
            return false;

        if (latinLetters is >= 1 and <= 3 && IsAllAsciiUpper(token))
            return false;

        return true;
    }

    static bool IsAllowedSingleCyrillicWord(string token)
    {
        // Common Russian / Ukrainian 1-letter function words (case-insensitive).
        return token.Equals("а", StringComparison.OrdinalIgnoreCase)
            || token.Equals("и", StringComparison.OrdinalIgnoreCase)
            || token.Equals("о", StringComparison.OrdinalIgnoreCase)
            || token.Equals("у", StringComparison.OrdinalIgnoreCase)
            || token.Equals("я", StringComparison.OrdinalIgnoreCase)
            || token.Equals("в", StringComparison.OrdinalIgnoreCase)
            || token.Equals("с", StringComparison.OrdinalIgnoreCase)
            || token.Equals("к", StringComparison.OrdinalIgnoreCase)
            || token.Equals("е", StringComparison.OrdinalIgnoreCase)
            || token.Equals("і", StringComparison.OrdinalIgnoreCase) // Ukrainian i
            || token.Equals("й", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsAllAsciiUpper(string token)
    {
        var sawLetter = false;
        foreach (var ch in token)
        {
            if (ch is >= 'a' and <= 'z')
                return false;
            if (ch is >= 'A' and <= 'Z')
                sawLetter = true;
        }

        return sawLetter;
    }

    static bool IsLetterAnyScript(int codePoint) =>
        OcrScriptQuality.IsLatinLetter(codePoint)
        || OcrScriptQuality.IsCyrillicScript(codePoint)
        || OcrScriptQuality.IsCjkScript(codePoint)
        || OcrScriptQuality.IsArabicScript(codePoint)
        || OcrScriptQuality.IsDevanagariScript(codePoint)
        || OcrScriptQuality.IsSoutheastAsianScript(codePoint)
        || char.IsLetter(char.ConvertFromUtf32(codePoint), 0);

    static string Truncate(string value) =>
        value.Length <= MaxTokenLength ? value : value[..MaxTokenLength];
}
