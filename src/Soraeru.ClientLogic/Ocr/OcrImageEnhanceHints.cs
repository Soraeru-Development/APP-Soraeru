namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// Pure heuristics for on-device OCR image prep (no bitmap deps).
/// Tesseract expects dark ink on light paper; screen photos of glowing text often need invert.
/// Light pastel ink on white (e.g. cyan speech-bubble text) needs contrast boost without invert.
/// </summary>
public static class OcrImageEnhanceHints
{
    /// <summary>Mean luminance below this (0–255) → treat as dark-dominant / likely light-on-dark.</summary>
    public const double DarkMeanLuminanceThreshold = 110;

    /// <summary>Upscale when the longer edge is shorter than this (screen crops / low-res).</summary>
    public const int UpscaleLongEdgeBelow = 900;

    /// <summary>
    /// Upscale when the shorter edge is below this — short UI strips / button rows often have a
    /// wide long edge but tiny glyph height, which drops 2–3 letter words in Tesseract.
    /// </summary>
    public const int UpscaleShortEdgeBelow = 320;

    /// <summary>
    /// Sample luminance std-dev below this on a bright image → low-contrast ink (pastel on white).
    /// </summary>
    public const double LowContrastStdDevThreshold = 48;

    /// <summary>Mean luminance above this (with low std-dev) → candidate for contrast stretch.</summary>
    public const double BrightMeanForContrastBoost = 150;

    public static bool ShouldInvertForOcr(double meanLuminance0To255) =>
        meanLuminance0To255 < DarkMeanLuminanceThreshold;

    /// <summary>
    /// Bright, flat images (light-blue text on white) need contrast stretch; skip when already dark
    /// (invert path handles those) or when std-dev shows strong existing contrast.
    /// </summary>
    public static bool ShouldBoostContrastForOcr(double meanLuminance0To255, double luminanceStdDev) =>
        !ShouldInvertForOcr(meanLuminance0To255)
        && meanLuminance0To255 >= BrightMeanForContrastBoost
        && luminanceStdDev >= 0
        && luminanceStdDev < LowContrastStdDevThreshold;

    public static bool ShouldUpscale(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return false;
        if (Math.Max(width, height) < UpscaleLongEdgeBelow)
            return true;
        return Math.Min(width, height) < UpscaleShortEdgeBelow;
    }

    /// <summary>User-facing hint when OCR returns empty after a forced script route.</summary>
    public static string EmptyResultGuidance(OcrScriptFamilyHint hint) =>
        hint switch
        {
            OcrScriptFamilyHint.Arabic =>
                "Tesseract 無法辨識文字。螢幕翻拍、發光或點陣字較難辨識；請改拍清楚實體字、避免反光，或改手動輸入。",
            _ =>
                "Tesseract 無法辨識文字。請換一張更清楚、對比明顯的圖，或改手動輸入。"
        };
}
