using System.Text;

namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Heuristic source-language guess from OCR text scripts.
/// Only returns codes the App language picker can select; otherwise <c>auto</c>.
/// Script ≠ language certainty (e.g. bare Han → auto; plain Latin → auto).
/// </summary>
public static class OcrSourceLanguageInference
{
    /// <summary>
    /// Letters strongly associated with Vietnamese orthography (đ / horns / breve / common precomposed).
    /// Tone-only Latin (é, à) is shared with other languages and is ignored here.
    /// </summary>
    const string VietnameseDistinctiveLetters =
        "đĐăĂâÂêÊôÔơƠưƯ" +
        "ắằẳẵặẮẰẲẴẶấầẩẫậẤẦẨẪẬếềểễệẾỀỂỄỆốồổỗộỐỒỔỖỘớờởỡợỚỜỞỠỢứừửữựỨỪỬỮỰ";

    /// <summary>
    /// Infer a picker language code: <c>ja</c>, <c>th</c>, <c>ko</c>, <c>vi</c>, or <c>auto</c>.
    /// Does not emit <c>tl</c> / <c>en</c> from script alone (Latin is ambiguous).
    /// </summary>
    public static string Infer(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "auto";

        var hangul = 0;
        var kana = 0;
        var thai = 0;
        var vietDistinctive = 0;

        foreach (var rune in text.EnumerateRunes())
        {
            var v = rune.Value;
            if (IsHangul(v))
            {
                hangul++;
                continue;
            }

            if (IsKana(v))
            {
                kana++;
                continue;
            }

            if (IsThai(v))
            {
                thai++;
                continue;
            }

            if (rune.Utf16SequenceLength == 1
                && VietnameseDistinctiveLetters.Contains(rune.ToString(), StringComparison.Ordinal))
            {
                vietDistinctive++;
            }
        }

        var scriptWinner = WinnerAmong(
            ("ko", hangul),
            ("ja", kana),
            ("th", thai));
        if (scriptWinner is not null)
            return scriptWinner;

        // Need a clear orthographic signal — one đ/ơ is enough for short OCR snippets.
        if (vietDistinctive >= 1)
            return "vi";

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
}
