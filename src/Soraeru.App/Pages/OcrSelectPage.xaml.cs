using Soraeru.ClientLogic.Ocr;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class OcrSelectPage : ContentPage
{
    private readonly IOcrSessionStore _ocrSession;
    private readonly IAnalyzeFlowStore _flow;
    private string? _selectedToken;
    private bool _suppressTextChanged;

    public OcrSelectPage(IOcrSessionStore ocrSession, IAnalyzeFlowStore flow)
    {
        InitializeComponent();
        _ocrSession = ocrSession;
        _flow = flow;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var path = _ocrSession.LocalImagePath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            ThumbImage.Source = ImageSource.FromFile(path);
            ImageCaptionLabel.Text = "已選圖片（僅本機 OCR）";
        }
        else
        {
            ThumbImage.Source = null;
            ImageCaptionLabel.Text = "未找到預覽圖";
        }

        _suppressTextChanged = true;
        OcrEditor.Text = _ocrSession.RecognizedText ?? string.Empty;
        _suppressTextChanged = false;
        RebuildTokenRadios(OcrEditor.Text);
    }

    void OnOcrTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged)
            return;

        _ocrSession.RecognizedText = e.NewTextValue;
        RebuildTokenRadios(e.NewTextValue);
    }

    void RebuildTokenRadios(string? text)
    {
        TokenList.Children.Clear();
        var tokens = OcrTextTokenizer.Tokenize(text);
        _selectedToken = null;

        if (tokens.Count == 0)
        {
            TokenList.Children.Add(new Label
            {
                Text = "尚無可選詞。請編輯上方文字，或以空白分隔單字／短語。",
                FontSize = 13,
                TextColor = Colors.Gray
            });
            SelectionCountLabel.Text = "已選擇 0 個單字";
            return;
        }

        var first = true;
        foreach (var token in tokens)
        {
            var radio = new RadioButton
            {
                Content = token,
                GroupName = "OcrWord",
                IsChecked = first
            };
            radio.CheckedChanged += OnTokenCheckedChanged;
            TokenList.Children.Add(radio);
            if (first)
            {
                _selectedToken = token;
                first = false;
            }
        }

        UpdateSelectionCount();
    }

    void OnTokenCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value || sender is not RadioButton radio)
            return;

        _selectedToken = radio.Content?.ToString();
        UpdateSelectionCount();
    }

    void UpdateSelectionCount()
    {
        var count = string.IsNullOrWhiteSpace(_selectedToken) ? 0 : 1;
        SelectionCountLabel.Text = $"已選擇 {count} 個單字";
    }

    async void OnReselectClicked(object? sender, EventArgs e) =>
        await Routes.BackAsync();

    async void OnManualClicked(object? sender, EventArgs e) =>
        await Routes.GoAsync(Routes.WordInput);

    async void OnAnalyzeClicked(object? sender, EventArgs e)
    {
        if (!OcrAnalyzeSelection.TryResolve(_selectedToken, out var text, out var error))
        {
            await DisplayAlertAsync("提醒", error ?? OcrAnalyzeSelection.ErrorNothingSelected, "了解");
            return;
        }

        // Persist any user correction for back-navigation.
        _ocrSession.RecognizedText = OcrEditor.Text;

        _flow.PendingRequest = new AnalyzeRequestDto(
            text!,
            SourceLanguage: "auto",
            MemoryLanguage: "zh-TW",
            NotationPreference: "bopomofo",
            ForceRefresh: false);
        _flow.ClearError();

        await Routes.GoAsync(Routes.Analyzing);
    }
}
