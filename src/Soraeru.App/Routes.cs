using Soraeru.ClientLogic.Legal;

namespace Soraeru;

public static class Routes
{
    public const string Splash = "SplashPage";
    public const string Login = "LoginPage";
    public const string Register = "RegisterPage";
    public const string ForgotPassword = "ForgotPasswordPage";
    public const string Onboarding = "OnboardingPage";
    public const string Main = "main";
    public const string Home = "HomePage";
    public const string WordInput = "WordInputPage";
    public const string ImagePick = "ImagePickPage";
    public const string OcrSelect = "OcrSelectPage";
    public const string Analyzing = "AnalyzingPage";
    public const string AnalysisResult = "AnalysisResultPage";
    public const string NotebookList = "NotebookListPage";
    public const string NotebookDetail = "NotebookDetailPage";
    public const string Settings = "SettingsPage";
    public const string LegalDocument = "LegalDocumentPage";

    public static Task GoAsync(string route) => Shell.Current.GoToAsync(route);

    public static Task GoToPrivacyPolicyAsync() =>
        GoAsync($"{LegalDocument}?doc={LegalDocuments.PrivacyDocKey}");

    public static Task GoToAiDisclaimerAsync() =>
        GoAsync($"{LegalDocument}?doc={LegalDocuments.AiDisclaimerDocKey}");

    public static Task GoAsync(string route, bool animate) => Shell.Current.GoToAsync(route, animate);

    public static Task BackAsync() => Shell.Current.GoToAsync("..");

    /// <summary>Absolute navigation to a main tab (shows Shell TabBar).</summary>
    public static Task GoToMainTabAsync(string pageRoute) =>
        Shell.Current.GoToAsync($"//{Main}/{pageRoute}");
}
