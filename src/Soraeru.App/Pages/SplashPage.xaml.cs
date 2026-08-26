using Microsoft.Extensions.DependencyInjection;

using Soraeru.ClientLogic.Auth;
using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class SplashPage : ContentPage
{
    private bool _navigated;
    private CancellationTokenSource? _dotCts;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = AmbientBackground.StartAsync();
        if (_navigated)
            return;

        _ = PlayEntranceAsync();

        await Task.Delay(900);

        // Logout may have already pushed Login onto this root; don't fight the stack.
        if (Shell.Current.CurrentState.Location.OriginalString.Contains(Routes.Login, StringComparison.Ordinal))
        {
            _navigated = true;
            return;
        }

        _navigated = true;

        var services = Handler?.MauiContext?.Services
            ?? Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
        {
            await Routes.GoAsync(Routes.Login);
            return;
        }

        var session = services.GetRequiredService<IAuthSessionStore>();
        var api = services.GetRequiredService<ISoraeruApiClient>();
        var notebook = services.GetRequiredService<LocalNotebookService>();

        var hasSession = await session.HasSessionAsync();
        if (!hasSession)
        {
            var noSession = SessionAuthGate.DecideSplash(false, MeProbeResult.Unauthorized, false);
            await Routes.GoAsync(MapSplashRoute(noSession.Destination));
            return;
        }

        MeProbeResult probe;
        bool? serverOnboarding = null;
        MeProfileDto? me = null;
        try
        {
            me = await api.GetMeAsync();
            if (me is null)
            {
                probe = MeProbeResult.Unauthorized;
            }
            else
            {
                probe = MeProbeResult.Ok;
                serverOnboarding = me.OnboardingCompleted;
            }
        }
        catch
        {
            probe = MeProbeResult.Unreachable;
        }

        var localOnboarding = serverOnboarding
            ?? await session.GetOnboardingCompletedAsync();
        var decision = SessionAuthGate.DecideSplash(true, probe, localOnboarding);

        if (decision.ClearLocalNotebook)
            await notebook.ClearLocalNotebookAsync();
        if (decision.ClearSession)
            await session.ClearAsync();

        if (probe == MeProbeResult.Ok && me is not null)
        {
            var previousUserId = await session.GetUserIdAsync();
            await SignInNotebookIsolation.ApplyAsync(notebook, previousUserId, me.UserId);

            var token = await session.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                await session.SetSessionAsync(token, me.UserId, me.Email, me.OnboardingCompleted);
            }
        }

        // Open-App sync (also hooked on Window.Resumed in App).
        if (decision.Destination is SplashDestination.Home or SplashDestination.Onboarding)
        {
            var sync = services.GetService<NotebookSyncCoordinator>();
            if (sync is not null)
            {
                try
                {
                    await sync.SyncAsync();
                }
                catch
                {
                    // Best-effort; offline / unavailable mirror is fine.
                }
            }

            services.GetService<NotebookListRefreshGate>()?.NotifyDataMayHaveChanged();
            if (Shell.Current is AppShell shell)
                shell.ResetNotebookListPage();
        }

        await Routes.GoAsync(MapSplashRoute(decision.Destination));
    }

    static string MapSplashRoute(SplashDestination destination) =>
        destination switch
        {
            SplashDestination.Home => $"//{Routes.Main}/{Routes.Home}",
            SplashDestination.Onboarding => Routes.Onboarding,
            _ => Routes.Login
        };

    protected override void OnDisappearing()
    {
        _dotCts?.Cancel();
        _dotCts?.Dispose();
        _dotCts = null;
        AmbientBackground.Stop();
        base.OnDisappearing();
    }

    /// <summary>Brand entrance + bounce dots (Stitch L00 timing).</summary>
    private async Task PlayEntranceAsync()
    {
        try
        {
            BrandMarkFrame.Opacity = 0;
            BrandMarkFrame.TranslationY = 20;
            BrandMarkFrame.Scale = 0.95;
            TitleLabel.Opacity = 0;
            TitleLabel.TranslationY = 15;
            TaglineLabel.Opacity = 0;
            TaglineLabel.TranslationY = 15;
            BrandStack.Opacity = 1;

            await Task.Delay(100);
            await Task.WhenAll(
                BrandMarkFrame.FadeToAsync(1, 800, Easing.SpringOut),
                BrandMarkFrame.TranslateToAsync(0, 0, 800, Easing.SpringOut),
                BrandMarkFrame.ScaleToAsync(1, 800, Easing.SpringOut));

            await Task.Delay(100);
            _ = TitleLabel.FadeToAsync(1, 600, Easing.CubicOut);
            _ = TitleLabel.TranslateToAsync(0, 0, 600, Easing.CubicOut);
            await Task.Delay(100);
            _ = TaglineLabel.FadeToAsync(1, 600, Easing.CubicOut);
            _ = TaglineLabel.TranslateToAsync(0, 0, 600, Easing.CubicOut);

            await Task.Delay(300);
            await LoaderDots.FadeToAsync(1, 400, Easing.SinIn);
            StartDotBounce();
        }
        catch (ObjectDisposedException)
        {
            // Page navigated away mid-animation.
        }
    }

    void StartDotBounce()
    {
        _dotCts?.Cancel();
        _dotCts = new CancellationTokenSource();
        var token = _dotCts.Token;
        _ = BounceDotAsync(Dot1, 0, token);
        _ = BounceDotAsync(Dot2, 160, token);
        _ = BounceDotAsync(Dot3, 320, token);
    }

    static async Task BounceDotAsync(VisualElement dot, int delayMs, CancellationToken token)
    {
        try
        {
            if (delayMs > 0)
                await Task.Delay(delayMs, token);

            while (!token.IsCancellationRequested)
            {
                // HTML bounce: scale 0 → 1 → 0 over ~1.4s with ease-in-out.
                await dot.ScaleToAsync(1, 280, Easing.SinOut);
                await Task.Delay(200, token);
                await dot.ScaleToAsync(0, 280, Easing.SinIn);
                await Task.Delay(640, token);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (ObjectDisposedException)
        {
            // page torn down
        }
    }
}
