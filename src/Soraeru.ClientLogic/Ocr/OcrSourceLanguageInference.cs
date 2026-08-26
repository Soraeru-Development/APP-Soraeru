namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Heuristic source-language guess from OCR text scripts.
/// Returns curated ISO codes when script evidence is strong; otherwise auto.
/// </summary>
public static class OcrSourceLanguageInference
{
    const string VietnameseDistinctiveLetters =
        "đĐăĂâÂêÊôÔơƠưƯ" +
        "ắằẳẵặẮẰẲẴẶấầẩẫậẤẦẨẪẬếềểễệẾỀỂỄỆốồổỗộỐỒỔỖỘớờởỡợỚỜỞỠỢứừửữựỨỪỬỮỰ";

    public static string Infer(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "auto";

        var hangul = 0;
        var kana = 0;
        var thai = 0;
        var myanmar = 0;
        var khmer = 0;
        var lao = 0;
        var arabic = 0;
        var devanagari = 0;
        var cyrillic = 0;
        var vietDistinctive = 0;
        var spanishDistinctive = 0;

        foreach (var rune in text.EnumerateRunes())
        {
            var v = rune.Value;
            if (IsHangul(v)) { hangul++; continue; }
            if (IsKana(v)) { kana++; continue; }
            if (IsThai(v)) { thai++; continue; }
            if (IsMyanmar(v)) { myanmar++; continue; }
            if (IsKhmer(v)) { khmer++; continue; }
            if (IsLao(v)) { lao++; continue; }
            if (OcrScriptQuality.IsArabicScript(v)) { arabic++; continue; }
            if (OcrScriptQuality.IsDevanagariScript(v)) { devanagari++; continue; }
            if (IsCyrillic(v)) { cyrillic++; continue; }
            if (IsSpanishDistinctive(v)) { spanishDistinctive++; continue; }

            if (rune.Utf16SequenceLength == 1
                && VietnameseDistinctiveLetters.Contains(rune.ToString(), StringComparison.Ordinal))
            {
                vietDistinctive++;
            }
        }

        var scriptWinner = WinnerAmong(
            ("ko", hangul),
            ("ja", kana),
            ("th", thai),
            ("my", myanmar),
            ("km", khmer),
            ("lo", lao),
            ("ar", arabic),
            ("hi", devanagari),
            ("ru", cyrillic));
        if (scriptWinner is not null)
            return scriptWinner;

        if (vietDistinctive >= 1)
            return "vi";

        if (spanishDistinctive >= 1)
            return "es";

        return "auto";
    }

    static string? WinnerAmong(params (string Code, int Count)[] scores)
    {
        string? bestCode = null;
        var bestCount = 0;
        var tie = false;

        foreach (var (code, count) in scores)
        {
            if (count <= 0)
                continue;

            if (count > bestCount)
            {
                bestCode = code;
                bestCount = count;
                tie = false;
            }
            else if (count == bestCount)
            {
                tie = true;
            }
        }

        return tie || bestCount == 0 ? null : bestCode;
    }

    static bool IsHangul(int codePoint) =>
        codePoint is (>= 0x1100 and <= 0x11FF)
            or (>= 0x3130 and <= 0x318F)
            or (>= 0xA960 and <= 0xA97F)
            or (>= 0xAC00 and <= 0xD7A3)
            or (>= 0xD7B0 and <= 0xD7FF);

    static bool IsKana(int codePoint) =>
        codePoint is (>= 0x3040 and <= 0x309F)
            or (>= 0x30A0 and <= 0x30FF)
            or (>= 0x31F0 and <= 0x31FF)
            or (>= 0xFF66 and <= 0xFF9D);

    static bool IsThai(int codePoint) =>
        codePoint is >= 0x0E00 and <= 0x0E7F;

    static bool IsMyanmar(int codePoint) =>
        codePoint is >= 0x1000 and <= 0x109F;

    static bool IsKhmer(int codePoint) =>
        codePoint is (>= 0x1780 and <= 0x17FF) or (>= 0x19E0 and <= 0x19FF);

    static bool IsLao(int codePoint) =>
        codePoint is >= 0x0E80 and <= 0x0EFF;

    static bool IsCyrillic(int codePoint) =>
        OcrScriptQuality.IsCyrillicScript(codePoint);

    static bool IsSpanishDistinctive(int codePoint) =>
        codePoint is 'ñ' or 'Ñ' or '¿' or '¡';
}
