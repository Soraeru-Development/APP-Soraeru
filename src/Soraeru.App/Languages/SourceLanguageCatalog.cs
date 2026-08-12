namespace Soraeru.Languages;

/// <summary>
/// UI mapping for <c>DetectedLanguage</c> / source-language codes (chip、badge、icon glyph).
/// Keep extensions here so notebook filters and WordInput stay aligned.
/// </summary>
public static class SourceLanguageCatalog
{
    public sealed record LanguagePresentation(
        string Code,
        string ChipLabel,
        string BadgeCode,
        string IconGlyph,
        string BadgeBackgroundKey,
        string BadgeForegroundKey,
        string EnglishName);

    static readonly string[] PreferredOrder = ["en", "ja", "th", "ko", "vi", "tl"];

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

    static readonly Dictionary<string, LanguagePresentation> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new("en", "英", "ENG", "A", "SecondaryContainer", "OnSecondaryContainer", "English"),
        ["ja"] = new("ja", "日", "JPN", "あ", "TertiaryFixedDim", "OnTertiaryFixedVariant", "Japanese"),
        ["th"] = new("th", "泰", "THA", "ก", "SurfaceDim", "OnSurfaceVariant", "Thai"),
        ["ko"] = new("ko", "韓", "KOR", "한", "InfoContainer", "StatusInfo", "Korean"),
        ["vi"] = new("vi", "越", "VIE", "V", "SurfaceContainerHigh", "OnSurfaceVariant", "Vietnamese"),
        ["tl"] = new("tl", "菲", "TGL", "T", "SurfaceContainerHigh", "OnSurfaceVariant", "Tagalog"),
    };

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "und";

        var trimmed = code.Trim().ToLowerInvariant();
        return Aliases.TryGetValue(trimmed, out var alias) ? alias : trimmed;
    }

    public static LanguagePresentation Resolve(string? code)
    {
        var normalized = Normalize(code);
        if (Known.TryGetValue(normalized, out var known))
            return known;

        var badge = normalized.Length <= 3
            ? normalized.ToUpperInvariant()
            : normalized[..3].ToUpperInvariant();
        var chip = badge.Length <= 2 ? badge : badge[..2];
        var glyph = badge[..1];
        // Detail pill prefers a readable label, not a bare ISO code like "ru".
        var englishName = badge;
        return new(normalized, chip, badge, glyph, "SurfaceContainerHigh", "OnSurfaceVariant", englishName);
    }

    /// <summary>UI subtitle like「泰語 (Thai)」for known codes; empty for auto/unknown.</summary>
    public static string FormatAnalyzingSubtitle(string? code)
    {
        var normalized = Normalize(code);
        return normalized switch
        {
            "auto" or "und" => string.Empty,
            "en" => "英語 (English)",
            "ja" => "日語 (Japanese)",
            "th" => "泰語 (Thai)",
            "ko" => "韓語 (Korean)",
            "vi" => "越南語 (Vietnamese)",
            "tl" => "他加祿語 (Tagalog)",
            _ => Resolve(normalized).EnglishName
        };
    }

    /// <summary>
    /// Languages actually present in the notebook (one chip each). No「其他」bucket.
    /// </summary>
    public static IReadOnlyList<LanguagePresentation> PresentInLibrary(IEnumerable<string?> codes)
    {
        return codes
            .Select(Resolve)
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(Priority)
            .ThenBy(x => x.ChipLabel, StringComparer.Ordinal)
            .ToList();
    }

    static int Priority(LanguagePresentation language)
    {
        var index = Array.FindIndex(
            PreferredOrder,
            c => string.Equals(c, language.Code, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? 1_000 : index;
    }
}
