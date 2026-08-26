using System.Text;

namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Heuristics for OCR script quality / Cyrillic to Latin hallucination.
/// </summary>
public static class OcrScriptQuality
{
    public static bool ContainsCyrillic(string? text) => ContainsAny(text, IsCyrillicScript);

    public static bool ContainsArabic(string? text) => ContainsAny(text, IsArabicScript);

    public static bool ContainsDevanagari(string? text) => ContainsAny(text, IsDevanagariScript);

    public static bool ContainsSoutheastAsian(string? text) => ContainsAny(text, IsSoutheastAsianScript);

    public static bool LooksLikeCyrillicScriptHallucination(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || ContainsCyrillic(text))
            return false;

        foreach (var token in EnumerateLetterTokens(text))
        {
            if (token.Length >= 5 && HasInternalMixedCase(token))
                return true;
        }

        return false;
    }

    public static bool IsSuspiciousLatinOcr(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (ContainsCyrillic(text) || HasNonLatinScript(text))
            return false;

        if (LooksLikeCyrillicScriptHallucination(text))
            return true;

        var letters = 0;
        var vowels = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (!IsAsciiLetter(rune.Value))
                continue;

            letters++;
            if (IsAsciiVowel(rune.Value))
                vowels++;
        }

        if (letters is >= 3 and <= 8 && vowels == 0)
            return true;

        return false;
    }

    public static bool IsArabicScript(int codePoint) =>
        codePoint is (>= 0x0600 and <= 0x06FF)
            or (>= 0x0750 and <= 0x077F)
            or (>= 0x08A0 and <= 0x08FF)
            or (>= 0xFB50 and <= 0xFDFF)
            or (>= 0xFE70 and <= 0xFEFF);

    public static bool IsDevanagariScript(int codePoint) =>
        codePoint is (>= 0x0900 and <= 0x097F)
            or (>= 0xA8E0 and <= 0xA8FF);

    public static bool IsSoutheastAsianScript(int codePoint) =>
        IsThai(codePoint)
        || codePoint is (>= 0x1000 and <= 0x109F)
        || codePoint is (>= 0x0E80 and <= 0x0EFF)
        || codePoint is (>= 0x1780 and <= 0x17FF)
        || codePoint is (>= 0x19E0 and <= 0x19FF);

    public static bool IsCyrillicScript(int codePoint) =>
        codePoint is (>= 0x0400 and <= 0x04FF)
            or (>= 0x0500 and <= 0x052F)
            or (>= 0x2DE0 and <= 0x2DFF)
            or (>= 0xA640 and <= 0xA69F);

    public static bool IsCjkScript(int codePoint) =>
        IsHangul(codePoint) || IsKana(codePoint) || IsHan(codePoint);

    public static bool IsLatinLetter(int codePoint) =>
        codePoint is (>= 'A' and <= 'Z')
            or (>= 'a' and <= 'z')
            or (>= 0x00C0 and <= 0x024F);

    static bool ContainsAny(string? text, Func<int, bool> predicate)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var rune in text.EnumerateRunes())
        {
            if (predicate(rune.Value))
                return true;
        }

        return false;
    }

    static bool HasInternalMixedCase(string token)
    {
        var seenLower = false;
        var upperAfterLower = 0;

        foreach (var ch in token)
        {
            if (!char.IsLetter(ch))
                continue;

            if (char.IsLower(ch))
                seenLower = true;
            else if (char.IsUpper(ch) && seenLower)
                upperAfterLower++;
        }

        return upperAfterLower >= 2;
    }

    static IEnumerable<string> EnumerateLetterTokens(string text)
    {
        var sb = new StringBuilder();
        foreach (var rune in text.EnumerateRunes())
        {
            if (IsAsciiLetter(rune.Value))
            {
                sb.Append(rune.ToString());
                continue;
            }

            if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }

        if (sb.Length > 0)
            yield return sb.ToString();
    }

    static bool HasNonLatinScript(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            var v = rune.Value;
            if (IsCyrillicScript(v)
                || IsHangul(v)
                || IsKana(v)
                || IsThai(v)
                || IsHan(v)
                || IsArabicScript(v)
                || IsDevanagariScript(v)
                || IsSoutheastAsianScript(v))
                return true;
        }

        return false;
    }

    static bool IsHangul(int codePoint) =>
        codePoint is (>= 0x1100 and <= 0x11FF)
            or (>= 0x3130 and <= 0x318F)
            or (>= 0xAC00 and <= 0xD7A3);

    static bool IsKana(int codePoint) =>
        codePoint is (>= 0x3040 and <= 0x309F)
            or (>= 0x30A0 and <= 0x30FF);

    static bool IsThai(int codePoint) =>
        codePoint is >= 0x0E00 and <= 0x0E7F;

    static bool IsHan(int codePoint) =>
        codePoint is (>= 0x4E00 and <= 0x9FFF)
            or (>= 0x3400 and <= 0x4DBF);

    static bool IsAsciiLetter(int codePoint) =>
        codePoint is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');

    static bool IsAsciiVowel(int codePoint)
    {
        var c = codePoint is >= 'A' and <= 'Z' ? codePoint + 32 : codePoint;
        return c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';
    }
}
