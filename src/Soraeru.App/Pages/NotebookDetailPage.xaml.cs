using Soraeru.ClientLogic.Notebook;

namespace Soraeru.Pages;

[QueryProperty(nameof(CardId), "cardId")]
public partial class NotebookDetailPage : ContentPage
{
    private readonly LocalNotebookService _notebook;
    private Guid? _cardId;

    public NotebookDetailPage(LocalNotebookService notebook)
    {
        InitializeComponent();
        _notebook = notebook;
    }

    public string CardId
    {
        set
        {
            if (Guid.TryParse(value, out var id))
                _cardId = id;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        if (_cardId is null)
        {
            SourceTextLabel.Text = "找不到單字卡";
            DeleteButton.IsEnabled = false;
            return;
        }

        try
        {
            var card = await _notebook.GetAsync(_cardId.Value);
            if (card is null)
            {
                SourceTextLabel.Text = "找不到單字卡";
                LanguageLabel.Text = string.Empty;
                DeleteButton.IsEnabled = false;
                return;
            }

            SourceTextLabel.Text = card.SourceText;
            LanguageLabel.Text = $"來源語言：{card.DetectedLanguage}";
            MeaningLabel.Text = $"詞義：{card.MeaningZh}";
            ReadingLabel.Text = $"正式讀音：{card.Pronunciation}";
            MnemonicLabel.Text = card.SelectedMnemonic;
            CreatedAtLabel.Text = $"建立時間：{card.CreatedAtUtc.ToLocalTime():yyyy-MM-dd}";
            DeleteButton.IsEnabled = await _notebook.CanWriteAsync();
        }
        catch (Exception ex)
        {
            SourceTextLabel.Text = "讀取失敗";
            LanguageLabel.Text = ex.Message;
            DeleteButton.IsEnabled = false;
        }
    }

    async void OnPlayClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("播放", "MVP 尚未接 TTS。", "了解");

    async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_cardId is null)
            return;

        if (!await _notebook.CanWriteAsync())
        {
            await DisplayAlertAsync("需要登入", "未登入時單字本為唯讀，無法刪除。", "了解");
            return;
        }

        var ok = await DisplayAlertAsync("刪除單字卡", "確定要刪除此單字卡？", "刪除", "取消");
        if (!ok)
            return;

        var result = await _notebook.DeleteAsync(_cardId.Value);
        if (!result.IsSuccess)
        {
            await DisplayAlertAsync("刪除失敗", result.Message ?? "無法刪除。", "了解");
            return;
        }

        await Routes.BackAsync();
    }
}
