using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ISoraeruApiClient _api;

    public ForgotPasswordPage(ISoraeruApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    async void OnSendClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlertAsync("提醒", "請輸入 Email。", "了解");
            return;
        }

        try
        {
            _ = await _api.RequestPasswordResetAsync(email);
            await DisplayAlertAsync(
                "已送出",
                "若此 Email 已註冊，重設連結會寫入 API 伺服器日誌（開發用 LoggingEmailSender）。",
                "了解");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("無法連線", $"請確認 API 已啟動。\n{ex.Message}", "了解");
        }
    }

    async void OnBackLoginClicked(object? sender, EventArgs e) =>
        await Routes.BackAsync();
}
