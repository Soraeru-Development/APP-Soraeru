namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Resolves the single OCR-selected token into text for the shared analyze pipeline.
/// </summary>
public static class OcrAnalyzeSelection
{
    public const string ErrorNothingSelected = "請選擇一個要分析的單字或短語。";

    public static bool TryResolve(string? selectedToken, out string? text, out string? error)
    {
        var trimmed = selectedToken?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            text = null;
            error = ErrorNothingSelected;
            return false;
        }

        text = trimmed.Length <= OcrTextTokenizer.MaxTokenLength
            ? trimmed
            : trimmed[..OcrTextTokenizer.MaxTokenLength];
        error = null;
        return true;
    }
}
