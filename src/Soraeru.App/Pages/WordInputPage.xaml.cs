using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class WordInputPage : ContentPage
{
    private readonly IAnalyzeFlowStore _flow;

    public WordInputPage(IAnalyzeFlowStore flow)
    {
        InitializeComponent();
        _flow = flow;
    }

    void OnWordTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue ?? string.Empty;
        if (text.Length > 50)
        {
            WordEditor.Text = text[..50];
            return;
        }

        CharCountLabel.Text = $"字數 {text.Length}／50";
    }

    async void OnAnalyzeClicked(object? sender, EventArgs e)
    {
        var text = WordEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlertAsync("提醒", "請輸入單字或短語。", "了解");
            return;
        }

        _flow.PendingRequest = new AnalyzeRequestDto(
            text,
            ResolveSourceLanguage(),
            "zh-TW",
            ResolveNotationPreference(),
            ForceRefresh: false);
        _flow.ClearError();

        await Routes.GoAsync(Routes.Analyzing);
    }

    string ResolveSourceLanguage() =>
        LanguagePicker.SelectedIndex switch
        {
            1 => "en",
            2 => "ja",
            3 => "th",
            4 => "ko",
            5 => "auto",
            _ => "auto"
        };

    string ResolveNotationPreference()
    {
        if (NotationRoman.IsChecked)
            return "roman";
        if (NotationMixed.IsChecked)
            return "mixed";
        return "bopomofo";
    }
}
