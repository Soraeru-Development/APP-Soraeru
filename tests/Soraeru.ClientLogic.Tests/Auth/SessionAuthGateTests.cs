using Shouldly;
using Soraeru.ClientLogic.Auth;

namespace Soraeru.ClientLogic.Tests.Auth;

public sealed class SessionAuthGateTests
{
    [Fact]
    public void Splash_when_token_present_and_api_unreachable_stays_offline_authenticated()
    {
        var decision = SessionAuthGate.DecideSplash(
            hasLocalSession: true,
            meProbe: MeProbeResult.Unreachable,
            localOnboardingCompleted: true);

        decision.Destination.ShouldBe(SplashDestination.Home);
        decision.ClearLocalNotebook.ShouldBeFalse();
        decision.ClearSession.ShouldBeFalse();
    }

    [Fact]
    public void Splash_when_token_present_and_GetMe_unauthorized_clears_session_keeps_local_notebook()
    {
        var decision = SessionAuthGate.DecideSplash(
            hasLocalSession: true,
            meProbe: MeProbeResult.Unauthorized,
            localOnboardingCompleted: true);

        decision.Destination.ShouldBe(SplashDestination.Login);
        decision.ClearLocalNotebook.ShouldBeFalse();
        decision.ClearSession.ShouldBeTrue();
    }

    [Fact]
    public void Settings_when_GetMe_unauthorized_clears_session_keeps_local_notebook()
    {
        var decision = SessionAuthGate.DecideSettingsProfile(MeProbeResult.Unauthorized);

        decision.ClearLocalNotebook.ShouldBeFalse();
        decision.ClearSession.ShouldBeTrue();
        decision.GoToLogin.ShouldBeTrue();
        decision.ShowProfile.ShouldBeFalse();
    }

    [Fact]
    public void DeleteAccount_success_clears_local_notebook_and_session()
    {
        var decision = SessionAuthGate.DecideAfterDeleteAccount(DeleteAccountApiResult.Success);

        decision.ClearLocalNotebook.ShouldBeTrue();
        decision.ClearSession.ShouldBeTrue();
    }

    [Fact]
    public void DeleteAccount_network_failure_keeps_local_state()
    {
        var decision = SessionAuthGate.DecideAfterDeleteAccount(DeleteAccountApiResult.Failed);

        decision.ClearLocalNotebook.ShouldBeFalse();
        decision.ClearSession.ShouldBeFalse();
    }

    /// <summary>
    /// Explicit logout must not wipe local SoT — multi-user rows remain; list filters by owner.
    /// Delete account clears only that owner's rows via DecideAfterDeleteAccount.
    /// </summary>
    [Fact]
    public void Logout_clears_session_but_keeps_local_notebook()
    {
        var decision = SessionAuthGate.DecideLogout();

        decision.ClearLocalNotebook.ShouldBeFalse();
        decision.ClearSession.ShouldBeTrue();
    }
}
