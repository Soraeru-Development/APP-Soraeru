using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class WordInputPage : ContentPage
{
    private readonly IAnalyzeFlowStore _flow;
    private readonly LocalNotebookService _notebook;

    public WordInputPage(IAnalyzeFlowStore flow, LocalNotebookService notebook)
    {
        InitializeComponent();
        _flow = flow;
        _notebook = notebook;
    }

    void OnWordTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue ?? string.Empty;
        if (text.Length > 50)
        {
            WordEditor.Text = text[..50];
            return;
        }

        CharCountLabel.Text = $"{text.Length} / 50";
    }

    void OnBopomofoRowTapped(object? sender, TappedEventArgs e) =>
        NotationBopomofo.IsChecked = true;

    void OnRomanRowTapped(object? sender, TappedEventArgs e) =>
        NotationRoman.IsChecked = true;

    void OnMixedRowTapped(object? sender, TappedEventArgs e) =>
        NotationMixed.IsChecked = true;

    async void OnAnalyzeClicked(object? sender, EventArgs e)
    {
        var text = WordEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlertAsync("提醒", "請輸入外語單字或短語。", "了解");
            return;
        }

        var sourceLanguage = ResolveSourceLanguage();
        var notation = ResolveNotationPreference();

        await AnalyzeEntryFlow.RouteLookupAsync(
            this,
            _notebook,
            _flow,
            text,
            sourceLanguage,
            memoryLanguage: "zh-TW",
            notation);
    }

    string ResolveSourceLanguage() =>
        LanguagePicker.SelectedIndex switch
        {
            1 => "ja",
            2 => "th",
            3 => "tl",
            4 => "ko",
            5 => "vi",
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
