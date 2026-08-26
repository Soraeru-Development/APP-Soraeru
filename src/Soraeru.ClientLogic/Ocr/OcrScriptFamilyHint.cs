namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Soft script-family hint chosen before OCR (not a full language wall).
/// </summary>
public enum OcrScriptFamilyHint
{
    Auto = 0,
    Latin = 1,
    Cyrillic = 2,
    Cjk = 3,
    Arabic = 4,
    Devanagari = 5,
    SoutheastAsian = 6,
    Other = 7
}
