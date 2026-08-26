namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Pure OCR engine routing decisions (no ML Kit / Tesseract deps).
/// App wires <c>HybridDeviceOcrService</c> from this plan.
/// </summary>
public static class OcrEngineRouter
{
    public const string AutoPrimaryLanguages = "tha+mya+lao+khm+ara+bod+rus+nep";
    public const string CyrillicPrimaryLanguages = "rus";
    public const string LatinPrimaryLanguages = "eng+spa+fil+vie";
    public const string CjkPrimaryLanguages = "jpn+kor+chi_tra+chi_sim";
    public const string ArabicPrimaryLanguages = "ara";
    public const string DevanagariPrimaryLanguages = "hin+nep";
    public const string SoutheastAsianPrimaryLanguages = "tha+mya+lao+khm";

    public static OcrEngineRoutePlan Plan(OcrScriptFamilyHint hint) =>
        hint switch
        {
            OcrScriptFamilyHint.Cyrillic => new OcrEngineRoutePlan(true, CyrillicPrimaryLanguages),
            OcrScriptFamilyHint.Latin => new OcrEngineRoutePlan(false, LatinPrimaryLanguages),
            OcrScriptFamilyHint.Cjk => new OcrEngineRoutePlan(false, CjkPrimaryLanguages),
            OcrScriptFamilyHint.Arabic => new OcrEngineRoutePlan(true, ArabicPrimaryLanguages),
            OcrScriptFamilyHint.Devanagari => new OcrEngineRoutePlan(false, DevanagariPrimaryLanguages),
            OcrScriptFamilyHint.SoutheastAsian => new OcrEngineRoutePlan(true, SoutheastAsianPrimaryLanguages),
            OcrScriptFamilyHint.Other => new OcrEngineRoutePlan(false, AutoPrimaryLanguages),
            _ => new OcrEngineRoutePlan(false, AutoPrimaryLanguages)
        };

    public static OcrScriptFamilyHint ResolveEffectiveHint(OcrScriptFamilyHint hint, string? ocrText)
    {
        if (hint != OcrScriptFamilyHint.Auto)
            return hint;
        var detected = DetectDominantScriptFamily(ocrText);
        return detected == OcrScriptFamilyHint.Auto ? hint : detected;
    }

    public static OcrScriptFamilyHint DetectDominantScriptFamily(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return OcrScriptFamilyHint.Auto;

        var arabic = 0;
        var devanagari = 0;
        var sea = 0;
        var cyrillic = 0;
        var cjk = 0;
        var latin = 0;

        foreach (var rune in text.EnumerateRunes())
        {
            var v = rune.Value;
            if (OcrScriptQuality.IsArabicScript(v)) arabic++;
            else if (OcrScriptQuality.IsDevanagariScript(v)) devanagari++;
            else if (OcrScriptQuality.IsSoutheastAsianScript(v)) sea++;
            else if (OcrScriptQuality.IsCyrillicScript(v)) cyrillic++;
            else if (OcrScriptQuality.IsCjkScript(v)) cjk++;
            else if (OcrScriptQuality.IsLatinLetter(v)) latin++;
        }

        return WinnerAmong(
            (OcrScriptFamilyHint.Arabic, arabic),
            (OcrScriptFamilyHint.Devanagari, devanagari),
            (OcrScriptFamilyHint.SoutheastAsian, sea),
            (OcrScriptFamilyHint.Cyrillic, cyrillic),
            (OcrScriptFamilyHint.Cjk, cjk),
            (OcrScriptFamilyHint.Latin, latin));
    }

    public static bool ShouldAcceptMlKitResult(string? fullText, OcrScriptFamilyHint hint)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return false;

        if (OcrScriptQuality.ContainsCyrillic(fullText))
            return true;

        if (hint == OcrScriptFamilyHint.Cyrillic)
            return false;

        if (hint == OcrScriptFamilyHint.Arabic)
            return OcrScriptQuality.ContainsArabic(fullText);

        if (hint == OcrScriptFamilyHint.SoutheastAsian)
            return OcrScriptQuality.ContainsSoutheastAsian(fullText);

        if (hint == OcrScriptFamilyHint.Devanagari)
            return OcrScriptQuality.ContainsDevanagari(fullText);

        if (OcrScriptQuality.LooksLikeCyrillicScriptHallucination(fullText))
            return false;

        if (OcrScriptQuality.IsSuspiciousLatinOcr(fullText))
            return false;

        return true;
    }

    /// <summary>
    /// Tess packs to ensure before Tesseract (primary langs + Latin on-demand proof packs).
    /// </summary>
    public static IReadOnlyList<string> RequiredTessPacks(OcrScriptFamilyHint hint)
    {
        var primary = Plan(hint).TesseractPrimaryLanguages
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (hint != OcrScriptFamilyHint.Latin)
            return primary;

        return primary
            .Concat(["deu", "fra"])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    static OcrScriptFamilyHint WinnerAmong(params (OcrScriptFamilyHint Family, int Count)[] scores)
    {
        var best = OcrScriptFamilyHint.Auto;
        var bestCount = 0;
        var tie = false;

        foreach (var (family, count) in scores)
        {
            if (count <= 0)
                continue;

            if (count > bestCount)
            {
                best = family;
                bestCount = count;
                tie = false;
            }
            else if (count == bestCount)
            {
                tie = true;
            }
        }

        return tie || bestCount == 0 ? OcrScriptFamilyHint.Auto : best;
    }
}

public sealed record OcrEngineRoutePlan(bool SkipMlKit, string TesseractPrimaryLanguages);
