using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class AnalysisResultPage : ContentPage
{
    private readonly IAnalyzeFlowStore _flow;
    private readonly LocalNotebookService _notebook;
    private int _selectedIndex;

    public AnalysisResultPage(IAnalyzeFlowStore flow, LocalNotebookService notebook)
    {
        InitializeComponent();
        _flow = flow;
        _notebook = notebook;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindResult(_flow.LastResult);
    }

    void BindResult(AnalyzeResultDto? result)
    {
        MnemonicsHost.Children.Clear();
        _selectedIndex = 0;

        if (result is null)
        {
            SourceTextLabel.Text = "—";
            LanguageLabel.Text = "尚無分析結果";
            MeaningLabel.Text = string.Empty;
            ReadingLabel.Text = string.Empty;
            DraftBadgeBanner.IsVisible = false;
            VerifiedBadgeBanner.IsVisible = false;
            NoticeLabel.Text = _flow.LastError ?? "請返回重新分析。";
            QuotaLabel.Text = string.Empty;
            return;
        }

        SourceTextLabel.Text = result.SourceText;
        LanguageLabel.Text = $"{result.LanguageDisplayName} · {result.SourceLanguage}";
        MeaningLabel.Text = $"詞義：{result.Meaning}";
        ReadingLabel.Text = $"正式讀音：{result.ReadingText}";
        var isVerified = string.Equals(result.MnemonicSource, "verified", StringComparison.OrdinalIgnoreCase);
        var isLlmDraft = !isVerified
            && (string.Equals(result.MnemonicSource, "llm_draft", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(result.MnemonicSource));
        DraftBadgeBanner.IsVisible = isLlmDraft;
        DraftBadgeLabel.Text = "AI 草稿／未經聽感核定";
        VerifiedBadgeBanner.IsVisible = isVerified;
        VerifiedBadgeLabel.Text = "聽感已核定／策展";
        NoticeLabel.Text = string.IsNullOrWhiteSpace(result.Notice)
            ? "⚠ 以下近似音僅供記憶，請以正式發音為準"
            : "⚠ " + result.Notice;
        QuotaLabel.Text = result.Cached
            ? $"快取結果 · 今日剩餘 {FormatQuota(result.RemainingDailyQuota)}"
            : $"今日剩餘 {FormatQuota(result.RemainingDailyQuota)}";

        for (var i = 0; i < result.Mnemonics.Count; i++)
        {
            var index = i;
            var m = result.Mnemonics[i];
            var radio = new RadioButton
            {
                Content = $"候選 {i + 1}　{m.DisplayText}",
                GroupName = "Mnemonic",
                IsChecked = i == 0
            };
            radio.CheckedChanged += (_, e) =>
            {
                if (e.Value)
                    _selectedIndex = index;
            };

            var card = new Border
            {
                Style = (Style)Application.Current!.Resources["CardBorder"],
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        radio,
                        new Label
                        {
                            Text = $"標記：{m.NotationText}",
                            Style = (Style)Application.Current.Resources["CaptionLabel"],
                            Margin = new Thickness(36, 0, 0, 0)
                        },
                        new Label
                        {
                            Text = $"提示：{m.Explanation}",
                            Style = (Style)Application.Current.Resources["CaptionLabel"],
                            Margin = new Thickness(36, 0, 0, 0)
                        }
                    }
                }
            };

            if (i == 1 && Application.Current.Resources.TryGetValue("SecondaryContainer", out var bg))
                card.BackgroundColor = (Color)bg;

            MnemonicsHost.Children.Add(card);
        }
    }

    static string FormatQuota(int remaining) =>
        remaining >= int.MaxValue / 2 ? "無限制" : remaining.ToString();

    async void OnPlayClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("播放", "MVP 尚未接 TTS。", "了解");

    async void OnFixLanguageClicked(object? sender, EventArgs e) =>
        await Routes.GoAsync(Routes.WordInput);

    async void OnRegenerateClicked(object? sender, EventArgs e)
    {
        var previous = _flow.PendingRequest;
        if (previous is null && _flow.LastResult is not null)
        {
            previous = new AnalyzeRequestDto(
                _flow.LastResult.SourceText,
                "auto",
                "zh-TW",
                _flow.LastResult.Mnemonics.FirstOrDefault()?.NotationType ?? "bopomofo");
        }

        if (previous is null)
        {
            await DisplayAlertAsync("提醒", "沒有可重新產生的文字。", "了解");
            return;
        }

        _flow.PendingRequest = previous with { ForceRefresh = true };
        await Routes.GoAsync(Routes.Analyzing);
    }

    async void OnSaveClicked(object? sender, EventArgs e)
    {
        var result = _flow.LastResult;
        if (result is null)
        {
            await DisplayAlertAsync("提醒", "尚無分析結果可儲存。", "了解");
            return;
        }

        if (result.Mnemonics.Count == 0)
        {
            await DisplayAlertAsync("提醒", "沒有可選的空耳候選。", "了解");
            return;
        }

        if (!await _notebook.CanWriteAsync())
        {
            await DisplayAlertAsync("需要登入", "登入後才能將候選存成本機單字卡（離線亦可寫入本機）。", "了解");
            return;
        }

        var index = Math.Clamp(_selectedIndex, 0, result.Mnemonics.Count - 1);
        var mnemonic = result.Mnemonics[index];

        try
        {
            var saved = await _notebook.SaveAsync(
                new SaveLocalWordCardCommand(
                    result.SourceText,
                    result.NormalizedText,
                    result.SourceLanguage,
                    result.Meaning,
                    result.ReadingText,
                    mnemonic.DisplayText));

            if (!saved.IsSuccess || saved.Value is null)
            {
                await DisplayAlertAsync("儲存失敗", saved.Message ?? "無法儲存單字卡。", "了解");
                return;
            }

            await Routes.GoAsync($"{Routes.NotebookDetail}?cardId={saved.Value.Id:D}");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("儲存失敗", ex.Message, "了解");
        }
    }
}
