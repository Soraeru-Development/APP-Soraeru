using Soraeru.ClientLogic.Notebook;
using Soraeru.ClientLogic.Ocr;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ISoraeruApiClient _api;
    private readonly IAuthSessionStore _session;
    private readonly LocalNotebookService _notebook;
    private readonly NotebookSyncCoordinator _sync;
    private readonly NotebookListRefreshGate _notebookListRefresh;
    private readonly IGoogleSignInService _googleSignIn;
    private readonly IOcrSessionStore _ocrSession;

    public LoginPage(
        ISoraeruApiClient api,
        IAuthSessionStore session,
        LocalNotebookService notebook,
        NotebookSyncCoordinator sync,
        NotebookListRefreshGate notebookListRefresh,
        IGoogleSignInService googleSignIn,
        IOcrSessionStore ocrSession)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _notebook = notebook;
        _sync = sync;
        _notebookListRefresh = notebookListRefresh;
        _googleSignIn = googleSignIn;
        _ocrSession = ocrSession;
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
        await Routes.GoToPrivacyPolicyAsync();

    async void OnAiDisclaimerClicked(object? sender, EventArgs e) =>
        await Routes.GoToAiDisclaimerAsync();

    async Task CompleteLoginAsync(AuthSessionDto session)
    {
        var previousUserId = await _session.GetUserIdAsync();
        await SignInNotebookIsolation.ApplyAsync(_notebook, previousUserId, session.UserId);

        await _session.SetSessionAsync(
            session.AccessToken,
            session.UserId,
            session.Email,
            session.OnboardingCompleted);

        // Login skips Splash; pull/push here so notebook is not empty until next resume.
        try
        {
            await _sync.SyncAsync();
        }
        catch
        {
            // Best-effort; offline / unavailable mirror is fine.
        }

        // Shell may keep a stale NotebookListPage; bump so the next tab show reloads.
        _notebookListRefresh.NotifyDataMayHaveChanged();
        if (Shell.Current is AppShell shell)
            shell.ResetNotebookListPage();

        var destination = OcrSessionRetention.ResolvePostLoginDestination(
            session.OnboardingCompleted,
            OcrSessionRetention.HasLiveRecognizedText(_ocrSession.RecognizedText));
        await Routes.GoAsync(destination switch
        {
            OcrPostLoginDestination.Onboarding => Routes.Onboarding,
            OcrPostLoginDestination.OcrSelect => ContinueOcrAfterLogin(),
            _ => $"//{Routes.Main}/{Routes.Home}"
        });
    }

    static string ContinueOcrAfterLogin()
    {
        if (Shell.Current is AppShell shell)
            shell.SuppressHomeRootResetOnce();
        return $"//{Routes.Main}/{Routes.Home}/{Routes.ImagePick}/{Routes.OcrSelect}";
    }
}
