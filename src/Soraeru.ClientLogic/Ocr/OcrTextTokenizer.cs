namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Splits on-device OCR full text into selectable word/phrase candidates (single-select UI).
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

        var result = new List<string>(parts.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var part in parts)
        {
            var token = Truncate(part);
            if (token.Length == 0)
                continue;
            if (!seen.Add(token))
                continue;
            result.Add(token);
        }

        return result;
    }

    static string Truncate(string value) =>
        value.Length <= MaxTokenLength ? value : value[..MaxTokenLength];
}
