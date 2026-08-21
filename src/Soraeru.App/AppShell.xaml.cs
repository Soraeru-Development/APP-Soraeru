using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Soraeru.Pages;

namespace Soraeru;

public partial class AppShell : Shell
{
    bool _mainTabsWired;
    bool _resettingHome;
    ShellItem? _watchedItem;

    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();

        // Shell keeps a per-tab navigation stack. Selecting 首頁 must always land on
        // L05 Home root — absolute //main/HomePage clears WordInput／Analyzing／….
        PropertyChanged += OnShellPropertyChanged;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        WireMainTabsIfNeeded();
        WatchCurrentItem();
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

    void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CurrentItem))
            return;

        WatchCurrentItem();
        _ = EnsureHomeRootIfSelectedAsync();
    }

    void WatchCurrentItem()
    {
        if (_watchedItem is not null)
            _watchedItem.PropertyChanged -= OnWatchedItemPropertyChanged;

        _watchedItem = CurrentItem;
        if (_watchedItem is not null)
            _watchedItem.PropertyChanged += OnWatchedItemPropertyChanged;
    }

    void OnWatchedItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ShellItem.CurrentItem))
            return;

        _ = EnsureHomeRootIfSelectedAsync();
    }

    /// <summary>
    /// Called when the Home tab is reselected (e.g. Android BottomNavigationView)
    /// while a pushed page is still on that tab's stack.
    /// </summary>
    public static Task OnHomeTabReselectedAsync() => Routes.GoToHomeRootAsync();

    async Task EnsureHomeRootIfSelectedAsync()
    {
        if (_resettingHome || !IsHomeSectionSelected() || IsAtHomeRoot())
            return;

        _resettingHome = true;
        try
        {
            await Routes.GoToHomeRootAsync();
        }
        finally
        {
            _resettingHome = false;
        }
    }

    bool IsHomeSectionSelected()
    {
        var section = CurrentItem?.CurrentItem;
        if (section is null)
            return false;

        if (string.Equals(section.Route, Routes.Home, StringComparison.Ordinal))
            return true;

        return section.Items.Any(c =>
            string.Equals(c.Route, Routes.Home, StringComparison.Ordinal));
    }

    bool IsAtHomeRoot()
    {
        var path = CurrentState?.Location?.OriginalString ?? string.Empty;
        var homeRoot = $"//{Routes.Main}/{Routes.Home}";
        return path.Equals(homeRoot, StringComparison.OrdinalIgnoreCase)
               || path.Equals($"//{Routes.Home}", StringComparison.OrdinalIgnoreCase);
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
