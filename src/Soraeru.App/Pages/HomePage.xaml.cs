using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class HomePage : ContentPage
{
    private const int UnlimitedDailyQuota = int.MaxValue;

    private readonly ISoraeruApiClient _api;

    public HomePage(ISoraeruApiClient api)
    {
        InitializeComponent();
        _api = api;
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            IsVisible = false,
            IsEnabled = false
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadQuotaAsync();
    }

    async Task LoadQuotaAsync()
    {
        QuotaLabel.Text = "今日剩餘 AI 次數：…";

        try
        {
            var me = await _api.GetMeAsync();
            if (me is null)
            {
                QuotaLabel.Text = "今日剩餘 AI 次數：—";
                return;
            }

            QuotaLabel.Text = FormatQuota(me);
        }
        catch
        {
            QuotaLabel.Text = "今日剩餘 AI 次數：—";
        }
    }

    static string FormatQuota(MeProfileDto me)
    {
        if (me.IsDeveloper || me.DailyQuota >= UnlimitedDailyQuota)
            return "今日剩餘 AI 次數：無限制";

        return $"今日剩餘 AI 次數：{me.RemainingDailyQuota}／{me.DailyQuota}";
    }

    async void OnSettingsClicked(object? sender, EventArgs e) =>
        await Routes.GoToMainTabAsync(Routes.Settings);

    async void OnWordInputTapped(object? sender, TappedEventArgs e) =>
        await Routes.GoAsync(Routes.WordInput);

    async void OnImagePickTapped(object? sender, TappedEventArgs e) =>
        await Routes.GoAsync(Routes.ImagePick);

    async void OnNotebookTapped(object? sender, TappedEventArgs e) =>
        await Routes.GoToMainTabAsync(Routes.NotebookList);
}
