namespace Soraeru.ClientLogic.Tts;

/// <summary>
/// Maps sourceLanguage codes to TTS language family / preferred BCP-47 tags.
/// </summary>
public static class FormalTtsLocale
{
    static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["eng"] = "en",
        ["english"] = "en",
        ["jpn"] = "ja",
        ["jp"] = "ja",
        ["japanese"] = "ja",
        ["tha"] = "th",
        ["thai"] = "th",
        ["kor"] = "ko",
        ["kr"] = "ko",
        ["korean"] = "ko",
        ["vie"] = "vi",
        ["vietnamese"] = "vi",
        ["tgl"] = "tl",
        ["fil"] = "tl",
        ["tagalog"] = "tl",
        ["filipino"] = "tl",
    };

    static readonly Dictionary<string, string> PreferredTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "en-US",
        ["ja"] = "ja-JP",
        ["th"] = "th-TH",
        ["ko"] = "ko-KR",
        ["vi"] = "vi-VN",
        ["tl"] = "fil-PH",
    };

    public static string NormalizeFamily(string? sourceLanguage)
    {
        if (string.IsNullOrWhiteSpace(sourceLanguage))
            return "und";

        var trimmed = sourceLanguage.Trim();
        // BCP-47: take primary subtag before '-' or '_'
        var primary = trimmed;
        var sep = trimmed.IndexOfAny(['-', '_']);
        if (sep > 0)
            primary = trimmed[..sep];

        primary = primary.ToLowerInvariant();
        if (Aliases.TryGetValue(primary, out var alias))
            return alias;

        if (string.Equals(primary, "auto", StringComparison.OrdinalIgnoreCase)
            || string.Equals(primary, "und", StringComparison.OrdinalIgnoreCase))
            return "und";

        return primary;
    }

    public static string PreferredTag(string languageFamily)
    {
        if (string.IsNullOrWhiteSpace(languageFamily)
            || string.Equals(languageFamily, "und", StringComparison.OrdinalIgnoreCase))
            return "und";

        return PreferredTags.TryGetValue(languageFamily, out var tag)
            ? tag
            : languageFamily.ToLowerInvariant();
    }

    /// <summary>
    /// Picks a device locale id whose language matches <paramref name="languageFamily"/>.
    /// Prefers exact PreferredTag match, then same language family. Returns null if none.
    /// For <c>und</c>, returns null so the caller uses the system default voice.
    /// </summary>
    public static string? PickLocaleId(
        string languageFamily,
        string preferredLanguageTag,
        IReadOnlyList<FormalTtsDeviceLocale> available)
    {
        if (available.Count == 0)
            return null;

        if (string.IsNullOrWhiteSpace(languageFamily)
            || string.Equals(languageFamily, "und", StringComparison.OrdinalIgnoreCase))
            return null;

        var preferred = preferredLanguageTag?.Trim() ?? string.Empty;

        FormalTtsDeviceLocale? exact = null;
        FormalTtsDeviceLocale? familyMatch = null;

        foreach (var locale in available)
        {
            var lang = (locale.Language ?? string.Empty).Trim();
            if (lang.Length == 0)
                continue;

            var localeFamily = NormalizeFamily(lang);
            if (!string.Equals(localeFamily, languageFamily, StringComparison.OrdinalIgnoreCase))
                continue;

            familyMatch ??= locale;

            var id = (locale.Id ?? string.Empty).Trim();
            if (id.Length > 0
                && preferred.Length > 0
                && string.Equals(id, preferred, StringComparison.OrdinalIgnoreCase))
            {
                exact = locale;
                break;
            }

            // Some engines put BCP-47 in Language instead of Id.
            if (preferred.Length > 0
                && string.Equals(lang, preferred, StringComparison.OrdinalIgnoreCase))
            {
                exact = locale;
                break;
            }
        }

        var chosen = exact ?? familyMatch;
        if (chosen is null)
            return null;

        return string.IsNullOrWhiteSpace(chosen.Id) ? chosen.Language : chosen.Id;
    }
}

/// <summary>Minimal device locale shape for pure matching (avoids MAUI types in ClientLogic).</summary>
public sealed record FormalTtsDeviceLocale(string Language, string? Id);
