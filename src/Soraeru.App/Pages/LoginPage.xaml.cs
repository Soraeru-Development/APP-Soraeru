using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ISoraeruApiClient _api;
    private readonly IAuthSessionStore _session;
    private readonly IGoogleSignInService _googleSignIn;

    public LoginPage(ISoraeruApiClient api, IAuthSessionStore session, IGoogleSignInService googleSignIn)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _googleSignIn = googleSignIn;
    }

    async void OnLoginClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("提醒", "請輸入 Email 與密碼。", "了解");
            return;
        }

        var result = await _api.LoginWithEmailAsync(email, password);
        if (!result.IsSuccess || result.Session is null)
        {
            var title = result.Failure == AuthFailureKind.Network ? "無法連線" : "登入失敗";
            await DisplayAlertAsync(title, result.Message ?? "登入失敗。", "了解");
            return;
        }

        await CompleteLoginAsync(result.Session);
    }

    async void OnRegisterClicked(object? sender, EventArgs e) =>
        await Routes.GoAsync(Routes.Register);

    async void OnForgotClicked(object? sender, EventArgs e) =>
        await Routes.GoAsync(Routes.ForgotPassword);

    async void OnGoogleClicked(object? sender, EventArgs e)
    {
        if (!_googleSignIn.IsSupported)
        {
            await DisplayAlertAsync("Google 登入", "請在 Android 上使用 Google 登入。", "了解");
            return;
        }

        var native = await _googleSignIn.SignInAsync();
        if (!native.IsSuccess || string.IsNullOrWhiteSpace(native.IdToken))
        {
            await DisplayAlertAsync("Google 登入", native.ErrorMessage ?? "Google 登入失敗。", "了解");
            return;
        }

        var result = await _api.LoginWithGoogleAsync(native.IdToken);
        if (!result.IsSuccess || result.Session is null)
        {
            var title = result.Failure == AuthFailureKind.Network ? "無法連線" : "登入失敗";
            await DisplayAlertAsync(title, result.Message ?? "Google 登入失敗。", "了解");
            return;
        }

        await CompleteLoginAsync(result.Session);
    }

    async void OnPrivacyClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("隱私權政策", "MVP 示範畫面，正式版本將提供完整政策連結。", "關閉");

    async Task CompleteLoginAsync(AuthSessionDto session)
    {
        await _session.SetSessionAsync(
            session.AccessToken,
            session.UserId,
            session.Email,
            session.OnboardingCompleted);

        await Routes.GoAsync(session.OnboardingCompleted ? Routes.Home : Routes.Onboarding);
    }
}
