using Soraeru.ClientLogic.Auth;
using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class SettingsPage : ContentPage
{
    private const int UnlimitedDailyQuota = int.MaxValue;

    private readonly ISoraeruApiClient _api;
    private readonly IAuthSessionStore _session;
    private readonly LocalNotebookService _notebook;

    public SettingsPage(ISoraeruApiClient api, IAuthSessionStore session, LocalNotebookService notebook)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _notebook = notebook;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfileAsync();
    }

    async Task LoadProfileAsync()
    {
        StatusLabel.IsVisible = true;
        StatusLabel.Text = "載入中…";

        if (!await _session.HasSessionAsync())
        {
            StatusLabel.Text = "尚未登入。";
            await GoToLoginAsync();
            return;
        }

        try
        {
            var me = await _api.GetMeAsync();
            var probe = me is null ? MeProbeResult.Unauthorized : MeProbeResult.Ok;
            var decision = SessionAuthGate.DecideSettingsProfile(probe);
            if (decision.ClearLocalNotebook)
                await _notebook.ClearLocalNotebookAsync();
            if (decision.ClearSession)
                await _session.ClearAsync();
            if (decision.GoToLogin)
            {
                StatusLabel.Text = "無法載入帳號資料，請重新登入。";
                await GoToLoginAsync();
                return;
            }

            DisplayNameLabel.Text = $"名稱：{me!.DisplayName}";
            EmailLabel.Text = $"Email：{MaskEmail(me.Email)}";
            LoginProviderLabel.Text = $"登入方式：{FormatLoginProviders(me)}";
            QuotaLabel.Text = FormatQuota(me);
            PlanLabel.Text = $"方案：{FormatPlanTier(me.PlanTier, me.IsDeveloper)}";
            StatusLabel.IsVisible = false;
        }
        catch (Exception ex)
        {
            var decision = SessionAuthGate.DecideSettingsProfile(MeProbeResult.Unreachable);
            if (decision.ClearLocalNotebook)
                await _notebook.ClearLocalNotebookAsync();
            if (decision.ClearSession)
                await _session.ClearAsync();
            if (decision.GoToLogin)
            {
                await GoToLoginAsync();
                return;
            }

            StatusLabel.Text = $"無法連線 API。\n{ex.Message}";
        }
    }

    async void OnOnboardingClicked(object? sender, EventArgs e) =>
        await Routes.GoAsync(Routes.Onboarding);

    async void OnPrivacyClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("隱私權政策", "MVP 示範畫面，正式版本將提供完整政策連結。", "關閉");

    async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync("登出", "確定要登出嗎？", "登出", "取消");
        if (!ok)
            return;

        await _notebook.ClearLocalNotebookAsync();
        await _session.ClearAsync();
        await GoToLoginAsync();
    }

    async void OnDeleteAccountClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync(
            "刪除帳號",
            "將永久刪除雲端單字本與帳號，並清除本機單字本與登入狀態。此操作無法復原。",
            "刪除帳號",
            "取消");
        if (!ok)
            return;

        StatusLabel.IsVisible = true;
        StatusLabel.Text = "正在刪除帳號…";

        var apiResult = await _api.DeleteAccountAsync();
        var gateResult = apiResult.Failure switch
        {
            DeleteAccountFailureKind.None => DeleteAccountApiResult.Success,
            DeleteAccountFailureKind.Unauthorized => DeleteAccountApiResult.Unauthorized,
            _ => DeleteAccountApiResult.Failed
        };

        var decision = SessionAuthGate.DecideAfterDeleteAccount(gateResult);
        if (decision.ClearLocalNotebook)
            await _notebook.ClearLocalNotebookAsync();
        if (decision.ClearSession)
            await _session.ClearAsync();

        if (apiResult.IsSuccess || apiResult.Failure == DeleteAccountFailureKind.Unauthorized)
        {
            await GoToLoginAsync();
            return;
        }

        StatusLabel.Text = apiResult.Message ?? "刪除帳號失敗。";
    }

    static Task GoToLoginAsync() =>
        Shell.Current.GoToAsync($"//{Routes.Splash}/{Routes.Login}");

    static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0)
            return email;

        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length == 1 ? local : local[..1];
        return $"{visible}***@{domain}";
    }

    static string FormatQuota(MeProfileDto me)
    {
        if (me.IsDeveloper || me.DailyQuota >= UnlimitedDailyQuota)
            return "今日剩餘：無限制";

        return $"今日剩餘：{me.RemainingDailyQuota}／{me.DailyQuota}";
    }

    static string FormatLoginProviders(MeProfileDto me)
    {
        if (me.HasGoogleSubject && me.HasPassword)
            return "Google／Email";
        if (me.HasGoogleSubject)
            return "Google";
        return "Email";
    }

    static string FormatPlanTier(string planTier, bool isDeveloper)
    {
        if (isDeveloper)
            return "開發者";

        return planTier.Equals("Free", StringComparison.OrdinalIgnoreCase)
            ? "免費方案"
            : planTier;
    }
}
