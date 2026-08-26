namespace Soraeru.ClientLogic.Ocr;

/// <summary>
/// When to keep vs drop the on-device OCR session so the learner can pick another
/// token from the same photo without re-running OCR.
/// </summary>
public enum OcrSessionLeaveTarget
{
    Home,
    WordInput,
    NewImagePick,
    AnalysisResult,
    NotebookDetail,
    Login,
    Analyzing,
    OcrSelect,
    LocalShortCircuit
}

public enum OcrPostLoginDestination
{
    Onboarding,
    OcrSelect,
    Home
}

public static class OcrSessionRetention
{
    public const string ContinueSamePhotoCta = "繼續選同圖其他字";

    public static bool ShouldShowContinueOcrCta(string? recognizedText, string? localImagePath)
    {
        _ = localImagePath;
        return HasLiveRecognizedText(recognizedText);
    }

    public static bool ShouldReturnToOcrSelectOnBack(string? recognizedText) =>
        HasLiveRecognizedText(recognizedText);

    public static bool ShouldClearOn(OcrSessionLeaveTarget target) =>
        target is OcrSessionLeaveTarget.Home
            or OcrSessionLeaveTarget.WordInput
            or OcrSessionLeaveTarget.NewImagePick;

    /// <summary>
    /// HomePage.OnAppearing also fires during nested Home-tab navigation.
    /// Only drop the session at the Home root, not on ImagePick／OcrSelect／Result.
    /// </summary>
    public static bool ShouldClearWhenHomeAppears(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return false;

        var path = location.Trim();
        return path.Equals("//main/HomePage", StringComparison.OrdinalIgnoreCase)
            || path.Equals("//HomePage", StringComparison.OrdinalIgnoreCase);
    }

    public static OcrPostLoginDestination ResolvePostLoginDestination(
        bool onboardingCompleted,
        bool ocrSessionActive)
    {
        if (!onboardingCompleted)
            return OcrPostLoginDestination.Onboarding;

        return ocrSessionActive
            ? OcrPostLoginDestination.OcrSelect
            : OcrPostLoginDestination.Home;
    }

    public static bool HasLiveRecognizedText(string? recognizedText) =>
        !string.IsNullOrWhiteSpace(recognizedText);
}
