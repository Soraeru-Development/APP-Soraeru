using Microsoft.Extensions.DependencyInjection;

using Soraeru.ClientLogic.Auth;
using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class SplashPage : ContentPage
{
    private bool _navigated;
    private CancellationTokenSource? _floatCts;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_navigated)
            return;

        _ = PlayEntranceAsync();
        _ = StartFloatingMotionAsync();

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
            var token = await session.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                await session.SetSessionAsync(token, me.UserId, me.Email, me.OnboardingCompleted);
            }
        }

        await Routes.GoAsync(MapSplashRoute(decision.Destination));
    }

    static string MapSplashRoute(SplashDestination destination) =>
        destination switch
        {
            SplashDestination.Home => Routes.Home,
            SplashDestination.Onboarding => Routes.Onboarding,
            _ => Routes.Login
        };

    protected override void OnDisappearing()
    {
        _floatCts?.Cancel();
        _floatCts?.Dispose();
        _floatCts = null;
        base.OnDisappearing();
    }

    /// <summary>Brand + copy fade-in, then loader (Stitch L00 entrance).</summary>
    private async Task PlayEntranceAsync()
    {
        try
        {
            await Task.WhenAll(
                FloatingLayer.FadeToAsync(0.72, 700, Easing.CubicOut),
                BrandStack.FadeToAsync(1, 650, Easing.CubicOut));

            await Loader.FadeToAsync(1, 400, Easing.SinIn);
        }
        catch (ObjectDisposedException)
        {
            // Page navigated away mid-animation.
        }
    }

    /// <summary>Gentle vertical drift on a few floating word cards.</summary>
    private async Task StartFloatingMotionAsync()
    {
        _floatCts?.Cancel();
        _floatCts?.Dispose();
        _floatCts = new CancellationTokenSource();
        var token = _floatCts.Token;

        var cards = new[] { FloatCard1, FloatCard2, FloatCard3, FloatCard4, FloatCard5, FloatCard6 };
        var tasks = cards.Select((card, i) =>
            DriftCardAsync(card, amplitude: 6 + (i % 3) * 2, periodMs: (uint)(2200 + i * 180), token));
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected when leaving splash.
        }
    }

    private static async Task DriftCardAsync(VisualElement card, double amplitude, uint periodMs, CancellationToken token)
    {
        var half = periodMs / 2;
        while (!token.IsCancellationRequested)
        {
            await card.TranslateToAsync(0, -amplitude, half, Easing.SinInOut);
            if (token.IsCancellationRequested)
                break;
            await card.TranslateToAsync(0, amplitude, half, Easing.SinInOut);
        }
    }
}
