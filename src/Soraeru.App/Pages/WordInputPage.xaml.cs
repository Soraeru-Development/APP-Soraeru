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

        var match = await _notebook.FindActiveByLookupKeyAsync(text, sourceLanguage);
        var authenticated = await _notebook.CanWriteAsync();
        var decision = AnalyzeEntryGate.DecideLookup(match, authenticated);

        if (decision.Kind == AnalyzeEntryKind.OpenLocalDetail && decision.CardId is { } cardId)
        {
            await Routes.GoAsync($"{Routes.NotebookDetail}?cardId={cardId:D}");
            return;
        }

        if (decision.Kind == AnalyzeEntryKind.RequireLogin)
        {
            await DisplayAlertAsync("需要登入", "登入後才能分析新單字。", "了解");
            await Routes.GoAsync(Routes.Login);
            return;
        }

        _flow.PendingRequest = new AnalyzeRequestDto(
            text,
            sourceLanguage,
            "zh-TW",
            notation,
            ForceRefresh: decision.ForceRefresh);
        _flow.ClearError();

        await Routes.GoAsync(Routes.Analyzing);
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
