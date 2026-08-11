namespace Soraeru;

public static class Routes
{
    public const string Splash = "SplashPage";
    public const string Login = "LoginPage";
    public const string Register = "RegisterPage";
    public const string ForgotPassword = "ForgotPasswordPage";
    public const string Onboarding = "OnboardingPage";
    public const string Home = "HomePage";
    public const string WordInput = "WordInputPage";
    public const string ImagePick = "ImagePickPage";
    public const string OcrSelect = "OcrSelectPage";
    public const string Analyzing = "AnalyzingPage";
    public const string AnalysisResult = "AnalysisResultPage";
    public const string NotebookList = "NotebookListPage";
    public const string NotebookDetail = "NotebookDetailPage";
    public const string Settings = "SettingsPage";

    public static Task GoAsync(string route) => Shell.Current.GoToAsync(route);

    public static Task GoAsync(string route, bool animate) => Shell.Current.GoToAsync(route, animate);

    public static Task BackAsync() => Shell.Current.GoToAsync("..");
}
