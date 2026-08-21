using Soraeru.ClientLogic.Notebook;
using Soraeru.ClientLogic.Ocr;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class OcrSelectPage : ContentPage
{
    private readonly IOcrSessionStore _ocrSession;
    private readonly IAnalyzeFlowStore _flow;
    private readonly LocalNotebookService _notebook;
    private string? _selectedToken;
    private bool _suppressTextChanged;
    private bool _suppressLanguagePickerChanged;
    private bool _languageTouchedByUser;

    public OcrSelectPage(
        IOcrSessionStore ocrSession,
        IAnalyzeFlowStore flow,
        LocalNotebookService notebook)
    {
        InitializeComponent();
        _ocrSession = ocrSession;
        _flow = flow;
        _notebook = notebook;
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
        MaybeApplyInferredSourceLanguage(OcrEditor.Text);
    }

    void OnOcrTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged)
            return;

        _ocrSession.RecognizedText = e.NewTextValue;
        RebuildTokenRadios(e.NewTextValue);
        MaybeApplyInferredSourceLanguage(e.NewTextValue);
    }

    void OnLanguagePickerChanged(object? sender, EventArgs e)
    {
        if (_suppressLanguagePickerChanged)
            return;

        _languageTouchedByUser = true;
        UpdateLanguageHelper(ResolveSourceLanguage(), inferred: false);
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

    async void OnManualClicked(object? sender, EventArgs e)
    {
        _ocrSession.Clear();
        await Routes.GoAsync(Routes.WordInput);
    }

    async void OnAnalyzeClicked(object? sender, EventArgs e)
    {
        if (!OcrAnalyzeSelection.TryResolve(_selectedToken, out var text, out var error))
        {
            await DisplayAlertAsync("提醒", error ?? OcrAnalyzeSelection.ErrorNothingSelected, "了解");
            return;
        }

        // Persist any user correction for back-navigation / analyze-failure retry.
        _ocrSession.RecognizedText = OcrEditor.Text;

        var sourceLanguage = ResolveSourceLanguage();
        var kind = await AnalyzeEntryFlow.RouteLookupAsync(
            this,
            _notebook,
            _flow,
            text!,
            sourceLanguage,
            memoryLanguage: "zh-TW",
            notationPreference: "bopomofo");

        // Keep session while Analyzing so hard-gate / network failure can return to L08.
        // Local detail / login leave the OCR flow — drop the uploaded preview.
        if (kind != AnalyzeEntryKind.ProceedToAnalyze)
            _ocrSession.Clear();
    }

    void MaybeApplyInferredSourceLanguage(string? text)
    {
        if (_languageTouchedByUser)
            return;

        var code = OcrSourceLanguageInference.Infer(text);
        var index = IndexForLanguageCode(code);

        _suppressLanguagePickerChanged = true;
        LanguagePicker.SelectedIndex = index;
        _suppressLanguagePickerChanged = false;
        UpdateLanguageHelper(code, inferred: true);
    }

    void UpdateLanguageHelper(string code, bool inferred)
    {
        if (string.Equals(code, "auto", StringComparison.OrdinalIgnoreCase))
        {
            LanguageHelperLabel.Text =
                "無法從文字可靠推斷時維持自動偵測。選定具體語言後，若本機已有同字卡會直接開詳情（不消耗分析額度）。";
            return;
        }

        var label = code switch
        {
            "ja" => "日文",
            "th" => "泰文",
            "tl" => "他加祿語",
            "ko" => "韓文",
            "vi" => "越南文",
            _ => code
        };

        LanguageHelperLabel.Text = inferred
            ? $"已依辨識文字預選為{label}（可手動更改）。選定具體語言後，若本機已有同字卡會直接開詳情（不消耗分析額度）。"
            : $"已選定{label}。若本機已有同字卡會直接開詳情（不消耗分析額度）。";
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

    static int IndexForLanguageCode(string code) =>
        code.Trim().ToLowerInvariant() switch
        {
            "ja" => 1,
            "th" => 2,
            "tl" => 3,
            "ko" => 4,
            "vi" => 5,
            _ => 0
        };
}
