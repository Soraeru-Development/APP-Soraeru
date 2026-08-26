using Soraeru.ClientLogic.Notebook;
using Soraeru.ClientLogic.Ocr;
using Soraeru.Languages;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class OcrSelectPage : ContentPage
{
    private readonly IOcrSessionStore _ocrSession;
    private readonly IAnalyzeFlowStore _flow;
    private readonly LocalNotebookService _notebook;
    private SourceLanguageSearchPicker? _languagePicker;
    private string? _selectedToken;
    private bool _suppressTextChanged;
    private bool _languageTouchedByUser;
    private bool _assistDismissed;

    public OcrSelectPage(
        IOcrSessionStore ocrSession,
        IAnalyzeFlowStore flow,
        LocalNotebookService notebook)
    {
        InitializeComponent();
        _ocrSession = ocrSession;
        _flow = flow;
        _notebook = notebook;
        _languagePicker = new SourceLanguageSearchPicker(
            LanguageSearchBar,
            LanguageList,
            SelectedLanguageLabel,
            code =>
            {
                _languageTouchedByUser = true;
                UpdateLanguageHelper(code, inferred: false);
            });
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
        RefreshAssistBanner();
    }

    void OnOcrTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged)
            return;

        _ocrSession.RecognizedText = e.NewTextValue;
        RebuildTokenRadios(e.NewTextValue);
        MaybeApplyInferredSourceLanguage(e.NewTextValue);
        RefreshAssistBanner();
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

    async void OnAssistClicked(object? sender, EventArgs e)
    {
        var original = OcrEditor.Text ?? string.Empty;
        if (!OcrTextAssistGate.ShouldSuggestAssist(original)
            && !string.Equals(_ocrSession.StatusMessage, "quality_suspicious", StringComparison.Ordinal))
        {
            AssistBanner.IsVisible = false;
            return;
        }

        var confirm = await DisplayAlertAsync(
            "文字建議（不上傳圖片）",
            "目前尚未接上獨立「OCR 文字校正」API。若繼續，請手動編輯上方辨識文字；正式建議需確認後才會套用，不會靜默覆寫。\n\n是否關閉此提示並自行校正？",
            "自行校正",
            "稍後");
        if (!confirm)
            return;

        _assistDismissed = true;
        AssistBanner.IsVisible = false;
        // Documented gap: thin suggest-fix endpoint (text-only) can replace this stub
        // without double-charging analyze quota when wired.
        _ = OcrTextAssistGate.BuildEditableSuggestionStub(original);
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
        await AnalyzeEntryFlow.RouteLookupAsync(
            this,
            _notebook,
            _flow,
            text!,
            sourceLanguage,
            memoryLanguage: "zh-TW",
            notationPreference: "bopomofo");
        // Keep RecognizedText after analyze / local short-circuit / login so the
        // learner can pick another token from the same photo.
    }

    void MaybeApplyInferredSourceLanguage(string? text)
    {
        if (_languageTouchedByUser || _languagePicker is null)
            return;

        var code = OcrSourceLanguageInference.Infer(text);
        _languagePicker.SetSelectedCode(code, notify: false);
        UpdateLanguageHelper(code, inferred: true);
    }

    void RefreshAssistBanner()
    {
        if (_assistDismissed)
        {
            AssistBanner.IsVisible = false;
            return;
        }

        var text = OcrEditor.Text;
        var flagged = string.Equals(_ocrSession.StatusMessage, "quality_suspicious", StringComparison.Ordinal)
            || OcrTextAssistGate.ShouldSuggestAssist(text);
        AssistBanner.IsVisible = flagged;
        if (flagged)
        {
            // Keep banner copy explicit: text-only, confirm required, no image upload.
            AssistBanner.IsVisible = true;
        }
    }

    void UpdateLanguageHelper(string code, bool inferred)
    {
        if (string.Equals(code, "auto", StringComparison.OrdinalIgnoreCase))
        {
            LanguageHelperLabel.Text =
                "無法從文字可靠推斷時維持自動偵測。選定具體語言後，若本機已有同字卡會直接開詳情（不消耗分析額度）。";
            return;
        }

        var label = SourceLanguageCatalog.FormatShortLabel(code);

        LanguageHelperLabel.Text = inferred
            ? $"已依辨識文字預選為{label}（可手動更改）。選定具體語言後，若本機已有同字卡會直接開詳情（不消耗分析額度）。"
            : $"已選定{label}。若本機已有同字卡會直接開詳情（不消耗分析額度）。";
    }

    string ResolveSourceLanguage() =>
        _languagePicker?.SelectedCode ?? "auto";
}
