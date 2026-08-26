namespace Soraeru.ClientLogic.Auth;

/// <summary>
/// Pure session / GetMe outcome decisions for Splash and Settings (no MAUI deps).
/// </summary>
public static class SessionAuthGate
{
    public static SplashSessionDecision DecideSplash(
        bool hasLocalSession,
        MeProbeResult meProbe,
        bool localOnboardingCompleted)
    {
        if (!hasLocalSession)
        {
            return new SplashSessionDecision(
                SplashDestination.Login,
                ClearLocalNotebook: false,
                ClearSession: false);
        }

        if (meProbe == MeProbeResult.Unreachable)
        {
            var dest = localOnboardingCompleted
                ? SplashDestination.Home
                : SplashDestination.Onboarding;
            return new SplashSessionDecision(
                dest,
                ClearLocalNotebook: false,
                ClearSession: false);
        }

        if (meProbe == MeProbeResult.Unauthorized)
        {
            // Session expired: clear session only. Keep on-device multi-user SoT rows.
            return new SplashSessionDecision(
                SplashDestination.Login,
                ClearLocalNotebook: false,
                ClearSession: true);
        }

        // Ok → continue online (implemented when tests require destination from onboarding flag).
        var online = localOnboardingCompleted
            ? SplashDestination.Home
            : SplashDestination.Onboarding;
        return new SplashSessionDecision(
            online,
            ClearLocalNotebook: false,
            ClearSession: false);
    }

    public static SettingsProfileDecision DecideSettingsProfile(MeProbeResult meProbe)
    {
        if (meProbe == MeProbeResult.Unauthorized)
        {
            return new SettingsProfileDecision(
                ShowProfile: false,
                GoToLogin: true,
                ClearLocalNotebook: false,
                ClearSession: true);
        }

        if (meProbe == MeProbeResult.Unreachable)
        {
            return new SettingsProfileDecision(
                ShowProfile: false,
                GoToLogin: false,
                ClearLocalNotebook: false,
                ClearSession: false);
        }

        return new SettingsProfileDecision(
            ShowProfile: true,
            GoToLogin: false,
            ClearLocalNotebook: false,
            ClearSession: false);
    }

    /// <summary>
    /// Explicit Settings logout: clear session only. Keep local SoT for all owners on device;
    /// re-login filters by OwnerUserId. Delete account clears that owner's rows via
    /// <see cref="DecideAfterDeleteAccount"/>.
    /// </summary>
    public static LocalCleanupDecision DecideLogout() =>
        new(ClearLocalNotebook: false, ClearSession: true);

    /// <summary>
    /// After DELETE /me — clear that owner's local rows (not other users) + session.
    /// </summary>
    public static LocalCleanupDecision DecideAfterDeleteAccount(DeleteAccountApiResult apiResult) =>
        apiResult switch
        {
            DeleteAccountApiResult.Success or DeleteAccountApiResult.Unauthorized =>
                new LocalCleanupDecision(ClearLocalNotebook: true, ClearSession: true),
            _ => new LocalCleanupDecision(ClearLocalNotebook: false, ClearSession: false)
        };
}

public enum MeProbeResult
{
    Ok,
    Unauthorized,
    Unreachable
}

public enum SplashDestination
{
    Login,
    Home,
    Onboarding
}

public sealed record SplashSessionDecision(
    SplashDestination Destination,
    bool ClearLocalNotebook,
    bool ClearSession);

public sealed record SettingsProfileDecision(
    bool ShowProfile,
    bool GoToLogin,
    bool ClearLocalNotebook,
    bool ClearSession);

public enum DeleteAccountApiResult
{
    Success,
    Unauthorized,
    Failed
}

public sealed record LocalCleanupDecision(bool ClearLocalNotebook, bool ClearSession);
