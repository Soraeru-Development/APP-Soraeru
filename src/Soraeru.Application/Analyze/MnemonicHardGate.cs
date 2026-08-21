using System.Text;
using System.Text.RegularExpressions;

namespace Soraeru.Application.Analyze;

/// <summary>
/// Post-schema hard gate for LLM mnemonic displayText (兒化 / Latin residue / non-learner scripts).
/// Allowed learner scripts: Han, Bopomofo, limited Latin attachments — not source-language scripts.
/// </summary>
public static partial class MnemonicHardGate
{
    public const string FailureCode = "HARD_GATE_FAILED";

    public const string DisallowedScriptReason =
        "近似音請用注音／羅馬拼音／漢字，不可含原文腳本（如韓文）。請重新產生。";

    private static readonly Regex MultiLatinRun = MultiLatinRunRegex();
    private static readonly Regex BannedLatinSyllable = BannedLatinSyllableRegex();

    public static bool TryValidateAll(
        IEnumerable<string> displayTexts,
        out string? reason)
    {
        foreach (var text in displayTexts)
        {
            if (!TryValidate(text, out reason))
            {
                return false;
            }
        }

        reason = null;
        return true;
    }

    public static bool TryValidate(string displayText, out string? reason)
    {
        if (string.IsNullOrWhiteSpace(displayText))
        {
            reason = "空耳 displayText 不可為空。";
            return false;
        }

        var text = displayText.Trim();

        if (text.Contains('兒', StringComparison.Ordinal) || text.Contains('尔', StringComparison.Ordinal)
            || text.Contains('爾', StringComparison.Ordinal))
        {
            reason = "空耳含不當兒化（兒／爾）。";
            return false;
        }

        if (BannedLatinSyllable.IsMatch(text))
        {
            reason = "空耳含拉丁音節殘渣。";
            return false;
        }

        if (MultiLatinRun.IsMatch(text))
        {
            reason = "空耳含連續拉丁字母片段。";
            return false;
        }

        if (HasDisallowedLatinAttachment(text))
        {
            reason = "空耳含孤立或不當拉丁殘渣。";
            return false;
        }

        if (HasDisallowedScript(text))
        {
            reason = DisallowedScriptReason;
            return false;
        }

        reason = null;
        return true;
    }

    private static bool HasDisallowedLatinAttachment(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (!IsLatinLetter(text[i]))
            {
                continue;
            }

            var prev = i > 0 ? text[i - 1] : '\0';
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            // Allowed: single Latin letter with at least one adjacent Han (e.g. 馬k / 普V頁).
            var prevHan = IsHan(prev);
            var nextHan = IsHan(next);
            if (!prevHan && !nextHan)
            {
                return true;
            }

            // Leading Latin before Han (k些) is treated as residue.
            if (!prevHan && nextHan)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDisallowedScript(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            var ch = rune.Value;
            if (IsHan(ch)
                || IsBopomofo(ch)
                || IsAllowedToneOrStressMark(ch)
                || IsAllowedSeparator((char)ch)
                || IsLatinLetter((char)ch))
            {
                continue;
            }

            // Digits / other letters (Cyrillic, Hangul, Kana, Thai, …) rejected.
            if (Rune.IsLetter(rune) || Rune.IsDigit(rune))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHan(char c) => IsHan((int)c);

    private static bool IsHan(int codePoint) =>
        (codePoint is >= 0x4E00 and <= 0x9FFF)
        || (codePoint is >= 0x3400 and <= 0x4DBF)
        || (codePoint is >= 0xF900 and <= 0xFAFF);

    /// <summary>Bopomofo (注音) letters — learner notation allowed in displayText.</summary>
    private static bool IsBopomofo(int codePoint) =>
        (codePoint is >= 0x3100 and <= 0x312F)
        || (codePoint is >= 0x31A0 and <= 0x31BF);

    private static bool IsAllowedToneOrStressMark(int codePoint) =>
        codePoint is '\u02C7' // ˇ
            or '\u02CA' // ˊ
            or '\u02CB' // ˋ
            or '\u02D9' // ˙
            or '\u02C9' // ˉ
            or '\u00B4' // ´
            or '\u02DC'; // ˜

    private static bool IsLatinLetter(char c) =>
        c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');

    private static bool IsAllowedSeparator(char c) =>
        c is '－' or '—' or '、' or '，' or ',' or '-' or '·' or '・' or ' ' or '\u3000'
            or '～' or '~' or '´';

    [GeneratedRegex(@"[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex MultiLatinRunRegex();

    [GeneratedRegex(@"\b(dei|te|la|day)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BannedLatinSyllableRegex();
}
