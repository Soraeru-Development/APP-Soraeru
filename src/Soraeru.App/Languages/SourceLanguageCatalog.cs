using LogicCatalog = Soraeru.ClientLogic.Languages.SourceLanguageCatalog;

namespace Soraeru.Languages;

/// <summary>
/// UI mapping for DetectedLanguage / source-language codes (chip, badge, icon glyph).
/// Curated ISO list + search live in ClientLogic; this layer adds presentation chrome.
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

    public static readonly string[] PickerCodes = LogicCatalog.FavoriteCodes;

    static readonly string[] PreferredOrder =
        ["en", "ja", "th", "ko", "vi", "tl", "ru", "es", "ar", "hi", "zh", "fr", "de"];

    static readonly Dictionary<string, (string Chip, string Badge, string Glyph, string Bg, string Fg)> Chrome =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = ("英", "ENG", "A", "SecondaryContainer", "OnSecondaryContainer"),
            ["ja"] = ("日", "JPN", "あ", "TertiaryFixedDim", "OnTertiaryFixedVariant"),
            ["th"] = ("泰", "THA", "ก", "SurfaceDim", "OnSurfaceVariant"),
            ["ko"] = ("韓", "KOR", "한", "InfoContainer", "StatusInfo"),
            ["vi"] = ("越", "VIE", "V", "SurfaceContainerHigh", "OnSurfaceVariant"),
            ["tl"] = ("菲", "TGL", "T", "SurfaceContainerHigh", "OnSurfaceVariant"),
            ["ru"] = ("俄", "RUS", "Я", "SecondaryContainer", "OnSecondaryContainer"),
            ["es"] = ("西", "SPA", "Ñ", "TertiaryFixedDim", "OnTertiaryFixedVariant"),
            ["zh"] = ("中", "ZHO", "中", "PrimaryContainer", "OnPrimaryContainer"),
            ["ar"] = ("阿", "ARA", "ع", "SurfaceDim", "OnSurfaceVariant"),
            ["hi"] = ("印", "HIN", "ह", "InfoContainer", "StatusInfo"),
            ["fr"] = ("法", "FRA", "F", "SecondaryContainer", "OnSecondaryContainer"),
            ["de"] = ("德", "DEU", "D", "TertiaryFixedDim", "OnTertiaryFixedVariant"),
            ["my"] = ("緬", "MYA", "မ", "SurfaceContainerHigh", "OnSurfaceVariant"),
            ["km"] = ("柬", "KHM", "ខ", "SurfaceContainerHigh", "OnSurfaceVariant"),
            ["lo"] = ("寮", "LAO", "ລ", "SurfaceContainerHigh", "OnSurfaceVariant"),
            ["bo"] = ("藏", "BOD", "བ", "SurfaceDim", "OnSurfaceVariant"),
        };

    public static string Normalize(string? code) => LogicCatalog.Normalize(code);

    public static LanguagePresentation Resolve(string? code)
    {
        var entry = LogicCatalog.Resolve(code);
        var normalized = entry.Code;

        if (Chrome.TryGetValue(normalized, out var chrome))
        {
            return new(
                normalized,
                chrome.Chip,
                chrome.Badge,
                chrome.Glyph,
                chrome.Bg,
                chrome.Fg,
                entry.EnglishName);
        }

        var badge = normalized.Length <= 3
            ? normalized.ToUpperInvariant()
            : normalized[..3].ToUpperInvariant();
        var chip = badge.Length <= 2 ? badge : badge[..2];
        var glyph = badge[..1];
        return new(normalized, chip, badge, glyph, "SurfaceContainerHigh", "OnSurfaceVariant", entry.EnglishName);
    }

    public static string FormatAnalyzingSubtitle(string? code)
    {
        var normalized = Normalize(code);
        if (normalized is "auto" or "und")
            return string.Empty;

        var entry = LogicCatalog.Resolve(normalized);
        if (LogicCatalog.CuratedCodes.Any(c => string.Equals(c, normalized, StringComparison.OrdinalIgnoreCase)))
            return $"{entry.ChineseName} ({entry.EnglishName})";

        return entry.EnglishName;
    }

    public static string CodeFromPickerIndex(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= PickerCodes.Length)
            return "auto";
        return PickerCodes[selectedIndex];
    }

    public static int PickerIndexFromCode(string? code)
    {
        var normalized = string.IsNullOrWhiteSpace(code) ? "auto" : Normalize(code);
        if (string.Equals(normalized, "und", StringComparison.OrdinalIgnoreCase))
            normalized = "auto";

        var index = Array.FindIndex(
            PickerCodes,
            c => string.Equals(c, normalized, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? 0 : index;
    }

    public static string FormatShortLabel(string? code)
    {
        var normalized = Normalize(code);
        if (normalized is "auto" or "und")
            return "自動偵測";

        var entry = LogicCatalog.Resolve(normalized);
        return entry.ChineseName.EndsWith('語')
            ? string.Concat(entry.ChineseName.AsSpan(0, entry.ChineseName.Length - 1), "文")
            : entry.ChineseName;
    }

    public static string FormatPickerLabel(string? code) => LogicCatalog.FormatPickerLabel(code);

    public static IReadOnlyList<LogicCatalog.Entry> Search(string? query) => LogicCatalog.Search(query);

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
