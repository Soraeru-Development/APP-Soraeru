using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly ISoraeruApiClient _api;
    private readonly IAuthSessionStore _session;
    private readonly LocalNotebookService _notebook;
    private readonly NotebookSyncCoordinator _sync;
    private readonly NotebookListRefreshGate _notebookListRefresh;

    public RegisterPage(
        ISoraeruApiClient api,
        IAuthSessionStore session,
        LocalNotebookService notebook,
        NotebookSyncCoordinator sync,
        NotebookListRefreshGate notebookListRefresh)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _notebook = notebook;
        _sync = sync;
        _notebookListRefresh = notebookListRefresh;
    }

    async void OnPrivacyClicked(object? sender, EventArgs e) =>
        await Routes.GoToPrivacyPolicyAsync();

    async void OnCreateClicked(object? sender, EventArgs e)
    {
        if (!PrivacyCheck.IsChecked)
        {
            await DisplayAlertAsync("提醒", "請先勾選已閱讀隱私權政策。", "了解");
            return;
        }

        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirm = ConfirmPasswordEntry.Text ?? string.Empty;
        var displayName = DisplayNameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("提醒", "請輸入 Email 與密碼。", "了解");
            return;
        }

        if (password.Length < 8)
        {
            await DisplayAlertAsync("提醒", "密碼至少需要 8 碼。", "了解");
            return;
        }

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            await DisplayAlertAsync("提醒", "兩次輸入的密碼不一致。", "了解");
            return;
        }

        var result = await _api.RegisterWithEmailAsync(email, password, displayName);
        if (!result.IsSuccess || result.Session is null)
        {
            var title = result.Failure == AuthFailureKind.Network ? "無法連線" : "註冊失敗";
            await DisplayAlertAsync(title, result.Message ?? "註冊失敗。", "了解");
            return;
        }

        var session = result.Session;
        var previousUserId = await _session.GetUserIdAsync();
        await SignInNotebookIsolation.ApplyAsync(_notebook, previousUserId, session.UserId);

        await _session.SetSessionAsync(
            session.AccessToken,
            session.UserId,
            session.Email,
            session.OnboardingCompleted);

        try
        {
            await _sync.SyncAsync();
        }
        catch
        {
            // Best-effort; offline / unavailable mirror is fine.
        }

        _notebookListRefresh.NotifyDataMayHaveChanged();
        if (Shell.Current is AppShell shell)
            shell.ResetNotebookListPage();

        await Routes.GoAsync(Routes.Onboarding);
    }
}
