using Soraeru.Pages;

namespace Soraeru;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    static void RegisterRoutes()
    {
        Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.ForgotPassword, typeof(ForgotPasswordPage));
        Routing.RegisterRoute(Routes.Onboarding, typeof(OnboardingPage));
        Routing.RegisterRoute(Routes.Home, typeof(HomePage));
        Routing.RegisterRoute(Routes.WordInput, typeof(WordInputPage));
        Routing.RegisterRoute(Routes.ImagePick, typeof(ImagePickPage));
        Routing.RegisterRoute(Routes.OcrSelect, typeof(OcrSelectPage));
        Routing.RegisterRoute(Routes.Analyzing, typeof(AnalyzingPage));
        Routing.RegisterRoute(Routes.AnalysisResult, typeof(AnalysisResultPage));
        Routing.RegisterRoute(Routes.NotebookList, typeof(NotebookListPage));
        Routing.RegisterRoute(Routes.NotebookDetail, typeof(NotebookDetailPage));
        Routing.RegisterRoute(Routes.Settings, typeof(SettingsPage));
    }
}
