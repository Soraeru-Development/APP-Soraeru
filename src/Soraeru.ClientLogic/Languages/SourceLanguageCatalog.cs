namespace Soraeru.ClientLogic.Languages;

/// <summary>
/// Curated ISO source-language options for OCR / WordInput pickers (searchable; not a tiny chip list).
/// Presentation chrome (badge colors) stays in the App layer.
/// </summary>
public static class SourceLanguageCatalog
{
    public sealed record Entry(
        string Code,
        string EnglishName,
        string ChineseName,
        string? NativeName = null);

    /// <summary>Favorites shown first when search is empty (index 0 = auto).</summary>
    public static readonly string[] FavoriteCodes =
    [
        "auto", "ja", "th", "tl", "ko", "vi", "ru", "es", "en", "ar", "hi", "zh"
    ];

    static readonly Entry[] CuratedEntries =
    [
        new("en", "English", "英語"),
        new("ja", "Japanese", "日語"),
        new("th", "Thai", "泰語"),
        new("tl", "Tagalog", "他加祿語"),
        new("ko", "Korean", "韓語"),
        new("vi", "Vietnamese", "越南語"),
        new("ru", "Russian", "俄語"),
        new("es", "Spanish", "西班牙語"),
        new("zh", "Chinese", "中文"),
        new("ar", "Arabic", "阿拉伯語", "العربية"),
        new("hi", "Hindi", "印地語", "हिन्दी"),
        new("fr", "French", "法語", "Français"),
        new("de", "German", "德語", "Deutsch"),
        new("pt", "Portuguese", "葡萄牙語", "Português"),
        new("it", "Italian", "義大利語", "Italiano"),
        new("id", "Indonesian", "印尼語"),
        new("ms", "Malay", "馬來語"),
        new("tr", "Turkish", "土耳其語"),
        new("pl", "Polish", "波蘭語"),
        new("uk", "Ukrainian", "烏克蘭語", "Українська"),
        new("nl", "Dutch", "荷蘭語"),
        new("sv", "Swedish", "瑞典語"),
        new("da", "Danish", "丹麥語"),
        new("no", "Norwegian", "挪威語"),
        new("fi", "Finnish", "芬蘭語"),
        new("el", "Greek", "希臘語", "Ελληνικά"),
        new("he", "Hebrew", "希伯來語", "עברית"),
        new("fa", "Persian", "波斯語", "فارسی"),
        new("bn", "Bengali", "孟加拉語", "বাংলা"),
        new("ta", "Tamil", "坦米爾語", "தமிழ்"),
        new("te", "Telugu", "泰盧固語"),
        new("mr", "Marathi", "馬拉提語"),
        new("gu", "Gujarati", "古吉拉特語"),
        new("kn", "Kannada", "卡納達語"),
        new("ml", "Malayalam", "馬拉亞拉姆語"),
        new("si", "Sinhala", "僧伽羅語"),
        new("my", "Burmese", "緬甸語", "မြန်မာ"),
        new("km", "Khmer", "高棉語", "ខ្មែរ"),
        new("lo", "Lao", "寮語", "ລາວ"),
        new("ne", "Nepali", "尼泊爾語"),
        new("bo", "Tibetan", "藏語", "བོད་ཡིག"),
    ];

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
        ["rus"] = "ru",
        ["russian"] = "ru",
        ["spa"] = "es",
        ["spanish"] = "es",
        ["es-es"] = "es",
        ["es-mx"] = "es",
        ["ara"] = "ar",
        ["arabic"] = "ar",
        ["hin"] = "hi",
        ["hindi"] = "hi",
        ["chi"] = "zh",
        ["zho"] = "zh",
        ["chinese"] = "zh",
        ["zh-tw"] = "zh",
        ["zh-cn"] = "zh",
        ["fra"] = "fr",
        ["fre"] = "fr",
        ["french"] = "fr",
        ["francais"] = "fr",
        ["français"] = "fr",
        ["deu"] = "de",
        ["ger"] = "de",
        ["german"] = "de",
        ["por"] = "pt",
        ["portuguese"] = "pt",
        ["ita"] = "it",
        ["italian"] = "it",
        ["ind"] = "id",
        ["indonesian"] = "id",
        ["may"] = "ms",
        ["msa"] = "ms",
        ["malay"] = "ms",
        ["tur"] = "tr",
        ["turkish"] = "tr",
        ["pol"] = "pl",
        ["polish"] = "pl",
        ["ukr"] = "uk",
        ["ukrainian"] = "uk",
        ["nld"] = "nl",
        ["dutch"] = "nl",
        ["swe"] = "sv",
        ["swedish"] = "sv",
        ["dan"] = "da",
        ["danish"] = "da",
        ["nor"] = "no",
        ["norwegian"] = "no",
        ["fin"] = "fi",
        ["finnish"] = "fi",
        ["ell"] = "el",
        ["gre"] = "el",
        ["greek"] = "el",
        ["heb"] = "he",
        ["hebrew"] = "he",
        ["fas"] = "fa",
        ["per"] = "fa",
        ["persian"] = "fa",
        ["farsi"] = "fa",
        ["ben"] = "bn",
        ["bengali"] = "bn",
        ["tam"] = "ta",
        ["tamil"] = "ta",
        ["tel"] = "te",
        ["telugu"] = "te",
        ["mar"] = "mr",
        ["marathi"] = "mr",
        ["guj"] = "gu",
        ["gujarati"] = "gu",
        ["kan"] = "kn",
        ["kannada"] = "kn",
        ["mal"] = "ml",
        ["malayalam"] = "ml",
        ["sin"] = "si",
        ["sinhala"] = "si",
        ["mya"] = "my",
        ["bur"] = "my",
        ["burmese"] = "my",
        ["khm"] = "km",
        ["khmer"] = "km",
        ["lao"] = "lo",
        ["nep"] = "ne",
        ["nepali"] = "ne",
        ["bod"] = "bo",
        ["tib"] = "bo",
        ["tibetan"] = "bo",
    };

    static readonly Dictionary<string, Entry> ByCode =
        CuratedEntries.ToDictionary(e => e.Code, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> CuratedCodes { get; } =
        CuratedEntries.Select(e => e.Code).ToArray();

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "und";

        var trimmed = code.Trim().ToLowerInvariant();
        return Aliases.TryGetValue(trimmed, out var alias) ? alias : trimmed;
    }

    public static Entry Resolve(string? code)
    {
        var normalized = Normalize(code);
        if (string.Equals(normalized, "auto", StringComparison.OrdinalIgnoreCase))
            return new Entry("auto", "Auto", "自動偵測");

        if (ByCode.TryGetValue(normalized, out var known))
            return known;

        var badge = normalized.Length <= 3
            ? normalized.ToUpperInvariant()
            : normalized[..3].ToUpperInvariant();
        return new Entry(normalized, badge, badge);
    }

    public static IReadOnlyList<Entry> Search(string? query)
    {
        var auto = new Entry("auto", "Auto", "自動偵測");
        if (string.IsNullOrWhiteSpace(query))
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ordered = new List<Entry>();
            foreach (var fav in FavoriteCodes)
            {
                var entry = Resolve(fav);
                if (seen.Add(entry.Code))
                    ordered.Add(entry);
            }

            foreach (var entry in CuratedEntries)
            {
                if (seen.Add(entry.Code))
                    ordered.Add(entry);
            }

            return ordered;
        }

        var q = query.Trim();
        var hits = CuratedEntries.Where(e => Matches(e, q)).ToList();
        if (Matches(auto, q) || q.Contains("自動", StringComparison.OrdinalIgnoreCase))
            return hits.Prepend(auto).ToList();

        return hits;
    }

    static bool Matches(Entry entry, string query)
    {
        if (entry.Code.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;
        if (entry.EnglishName.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;
        if (entry.ChineseName.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.IsNullOrEmpty(entry.NativeName)
            && entry.NativeName.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var (alias, code) in Aliases)
        {
            if (string.Equals(code, entry.Code, StringComparison.OrdinalIgnoreCase)
                && alias.Contains(query, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Picker display like「阿拉伯語 (Arabic)」.</summary>
    public static string FormatPickerLabel(string? code)
    {
        var entry = Resolve(code);
        if (string.Equals(entry.Code, "auto", StringComparison.OrdinalIgnoreCase))
            return entry.ChineseName;
        return $"{entry.ChineseName} ({entry.EnglishName})";
    }
}
