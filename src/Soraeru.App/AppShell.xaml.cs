using Microsoft.Extensions.DependencyInjection;
using Soraeru.Pages;

namespace Soraeru;

public partial class AppShell : Shell
{
    bool _mainTabsWired;

    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        WireMainTabsIfNeeded();
    }

    void WireMainTabsIfNeeded()
    {
        if (_mainTabsWired)
            return;

        var services = Handler?.MauiContext?.Services;
        if (services is null)
            return;

        HomeTab.Content = services.GetRequiredService<HomePage>();
        NotebookTab.Content = services.GetRequiredService<NotebookListPage>();
        SettingsTab.Content = services.GetRequiredService<SettingsPage>();
        _mainTabsWired = true;
    }

    static void RegisterRoutes()
    {
        Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.ForgotPassword, typeof(ForgotPasswordPage));
        Routing.RegisterRoute(Routes.Onboarding, typeof(OnboardingPage));
        Routing.RegisterRoute(Routes.LegalDocument, typeof(LegalDocumentPage));
        // Home / NotebookList / Settings are TabBar ShellContent (not registered routes).
        Routing.RegisterRoute(Routes.WordInput, typeof(WordInputPage));
        Routing.RegisterRoute(Routes.ImagePick, typeof(ImagePickPage));
        Routing.RegisterRoute(Routes.OcrSelect, typeof(OcrSelectPage));
        Routing.RegisterRoute(Routes.Analyzing, typeof(AnalyzingPage));
        Routing.RegisterRoute(Routes.AnalysisResult, typeof(AnalysisResultPage));
        Routing.RegisterRoute(Routes.NotebookDetail, typeof(NotebookDetailPage));
    }
}
