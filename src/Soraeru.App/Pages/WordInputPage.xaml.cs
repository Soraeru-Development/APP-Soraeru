using Soraeru.ClientLogic.Notebook;
using Soraeru.ClientLogic.Ocr;
using Soraeru.Languages;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class WordInputPage : ContentPage
{
    private readonly IAnalyzeFlowStore _flow;
    private readonly LocalNotebookService _notebook;
    private readonly IOcrSessionStore _ocrSession;
    private SourceLanguageSearchPicker? _languagePicker;

    public WordInputPage(IAnalyzeFlowStore flow, LocalNotebookService notebook, IOcrSessionStore ocrSession)
    {
        InitializeComponent();
        _flow = flow;
        _notebook = notebook;
        _ocrSession = ocrSession;
        _languagePicker = new SourceLanguageSearchPicker(
            LanguageSearchBar,
            LanguageList,
            SelectedLanguageLabel,
            _ => { });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (OcrSessionRetention.ShouldClearOn(OcrSessionLeaveTarget.WordInput))
            _ocrSession.Clear();
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
        _languagePicker?.SelectedCode ?? "auto";

    string ResolveNotationPreference()
    {
        if (NotationRoman.IsChecked)
            return "roman";
        if (NotationMixed.IsChecked)
            return "mixed";
        return "bopomofo";
    }
}
