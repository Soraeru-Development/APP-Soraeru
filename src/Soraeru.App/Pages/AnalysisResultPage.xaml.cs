using Microsoft.Maui.Controls.Shapes;
using Soraeru.ClientLogic.Analyze;
using Soraeru.ClientLogic.Notebook;
using Soraeru.ClientLogic.Ocr;
using Soraeru.ClientLogic.Tts;
using Soraeru.Languages;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class AnalysisResultPage : ContentPage
{
    private readonly IAnalyzeFlowStore _flow;
    private readonly LocalNotebookService _notebook;
    private readonly IFormalTtsService _tts;
    private readonly IOcrSessionStore _ocrSession;
    private readonly List<(Border Card, Border RadioDot)> _mnemonicVisuals = [];
    private int _selectedIndex;

    public AnalysisResultPage(
        IAnalyzeFlowStore flow,
        LocalNotebookService notebook,
        IFormalTtsService tts,
        IOcrSessionStore ocrSession)
    {
        InitializeComponent();
        _flow = flow;
        _notebook = notebook;
        _tts = tts;
        _ocrSession = ocrSession;

        WordCardBorder.Shadow = new Shadow
        {
            Brush = Colors.Black,
            Offset = new Point(0, 2),
            Radius = 8,
            Opacity = 0.05f
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindResult(_flow.LastResult);
        BindContinueOcrChrome();
    }

    protected override bool OnBackButtonPressed()
    {
        if (OcrSessionRetention.ShouldReturnToOcrSelectOnBack(_ocrSession.RecognizedText))
        {
            Dispatcher.Dispatch(() => Routes.GoToContinueOcrSelectAsync());
            return true;
        }

        return base.OnBackButtonPressed();
    }

    void BindContinueOcrChrome()
    {
        var show = OcrSessionRetention.ShouldShowContinueOcrCta(
            _ocrSession.RecognizedText,
            _ocrSession.LocalImagePath);
        ContinueOcrButton.Text = OcrSessionRetention.ContinueSamePhotoCta;
        ContinueOcrButton.IsVisible = show;
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            Command = new Command(async () => await OnOcrAwareBackAsync())
        });
    }

    async Task OnOcrAwareBackAsync()
    {
        if (OcrSessionRetention.ShouldReturnToOcrSelectOnBack(_ocrSession.RecognizedText))
            await Routes.GoToContinueOcrSelectAsync();
        else
            await Routes.BackAsync();
    }

    async void OnContinueOcrClicked(object? sender, EventArgs e) =>
        await Routes.GoToContinueOcrSelectAsync();

    void BindResult(AnalyzeResultDto? result)
    {
        MnemonicsHost.Children.Clear();
        _mnemonicVisuals.Clear();
        _selectedIndex = 0;

        if (result is null)
        {
            SourceTextLabel.Text = "—";
            LanguagePillLabel.Text = "尚無分析結果";
            MeaningLabel.Text = string.Empty;
            ReadingLabel.Text = string.Empty;
            DraftBadgeBanner.IsVisible = false;
            VerifiedBadgeBanner.IsVisible = false;
            NoticeLabel.Text = _flow.LastError ?? "請返回重新分析。";
            QuotaLabel.Text = string.Empty;
            ApplyRegenerateButtonState(remainingRegenerations: 0);
            return;
        }

        var lang = SourceLanguageCatalog.Resolve(result.SourceLanguage);
        var displayName = string.IsNullOrWhiteSpace(result.LanguageDisplayName)
            ? lang.EnglishName
            : result.LanguageDisplayName.Trim();
        var code = string.IsNullOrWhiteSpace(result.SourceLanguage)
            ? lang.Code
            : result.SourceLanguage.Trim();
        LanguagePillLabel.Text = $"{displayName} · {code}";

        SourceTextLabel.Text = result.SourceText;
        MeaningLabel.Text = string.IsNullOrWhiteSpace(result.Meaning)
            ? string.Empty
            : $"詞義：{result.Meaning}";
        MeaningLabel.IsVisible = !string.IsNullOrWhiteSpace(result.Meaning);

        ReadingLabel.Text = string.IsNullOrWhiteSpace(result.ReadingText)
            ? "正式讀音：—"
            : $"正式讀音：{result.ReadingText}";

        var isVerified = string.Equals(result.MnemonicSource, "verified", StringComparison.OrdinalIgnoreCase);
        var isLlmDraft = !isVerified
            && (string.Equals(result.MnemonicSource, "llm_draft", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(result.MnemonicSource));
        DraftBadgeBanner.IsVisible = isLlmDraft;
        DraftBadgeLabel.Text = "AI 草稿／未經聽感核定";
        VerifiedBadgeBanner.IsVisible = isVerified;
        VerifiedBadgeLabel.Text = "聽感已核定／策展";
        NoticeLabel.Text = string.IsNullOrWhiteSpace(result.Notice)
            ? "以下近似音僅供記憶，請以正式發音為準。"
            : result.Notice;
        QuotaLabel.Text = result.Cached
            ? $"快取結果 · 今日剩餘 {FormatQuota(result.RemainingDailyQuota)} · 可重產 {result.RemainingRegenerations}"
            : $"今日剩餘 {FormatQuota(result.RemainingDailyQuota)} · 可重產 {result.RemainingRegenerations}";
        ApplyRegenerateButtonState(result.RemainingRegenerations);

        var resources = Application.Current!.Resources;
        for (var i = 0; i < result.Mnemonics.Count; i++)
        {
            var index = i;
            var m = result.Mnemonics[i];
            var card = BuildMnemonicCard(m, index, resources);
            MnemonicsHost.Children.Add(card);
        }

        ApplyMnemonicSelection(_selectedIndex);
    }

    Border BuildMnemonicCard(AnalyzeMnemonicDto mnemonic, int index, ResourceDictionary resources)
    {
        var outlineVariant = (Color)resources["OutlineVariant"];
        var onSurface = (Color)resources["OnSurface"];
        var onSurfaceVariant = (Color)resources["OnSurfaceVariant"];
        var secondary = (Color)resources["Secondary"];
        var surfaceContainerHigh = (Color)resources["SurfaceContainerHigh"];
        var outline = (Color)resources["Outline"];

        var radioOuter = new Border
        {
            WidthRequest = 20,
            HeightRequest = 20,
            Stroke = outline,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            BackgroundColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 4, 0, 0)
        };

        // L10: display + notation read left-to-right. Stack notation under display so MAUI
        // WordWrap in a squeezed Auto column cannot force 注音／羅馬 into a vertical column.
        var displayLabel = new Label
        {
            Text = mnemonic.DisplayText,
            FontSize = 22,
            TextColor = onSurface,
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalOptions = LayoutOptions.Fill
        };

        var notationBadge = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            BackgroundColor = surfaceContainerHigh,
            Padding = new Thickness(8, 2),
            HorizontalOptions = LayoutOptions.Start,
            MaximumWidthRequest = 520,
            IsVisible = !string.IsNullOrWhiteSpace(mnemonic.NotationText),
            Content = new Label
            {
                Text = mnemonic.NotationText,
                FontSize = 14,
                TextColor = onSurfaceVariant,
                LineBreakMode = LineBreakMode.WordWrap,
                HorizontalOptions = LayoutOptions.Start
            }
        };

        var explanation = new FormattedString();
        explanation.Spans.Add(new Span
        {
            Text = "記憶技巧：",
            FontAttributes = FontAttributes.Bold,
            TextColor = secondary,
            FontSize = 16
        });
        explanation.Spans.Add(new Span
        {
            Text = string.IsNullOrWhiteSpace(mnemonic.Explanation) ? "—" : mnemonic.Explanation,
            TextColor = onSurfaceVariant,
            FontSize = 16
        });

        var body = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                displayLabel,
                notationBadge,
                new Label
                {
                    FormattedText = explanation,
                    LineBreakMode = LineBreakMode.WordWrap,
                    Margin = new Thickness(0, 4, 0, 0)
                }
            }
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Star)
            },
            ColumnSpacing = 16
        };
        row.Add(radioOuter, 0);
        row.Add(body, 1);

        var card = new Border
        {
            Stroke = outlineVariant,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = (Color)resources["Surface"],
            Padding = 16,
            Content = row
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            _selectedIndex = index;
            ApplyMnemonicSelection(index);
        };
        card.GestureRecognizers.Add(tap);

        _mnemonicVisuals.Add((card, radioOuter));
        return card;
    }

    void ApplyMnemonicSelection(int selectedIndex)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
            return;

        var primary = (Color)resources["Primary"];
        var outline = (Color)resources["Outline"];
        var outlineVariant = (Color)resources["OutlineVariant"];
        var surface = (Color)resources["Surface"];
        var primaryFixed = (Color)resources["PrimaryFixed"];
        var selectedBg = Color.FromRgba(primaryFixed.Red, primaryFixed.Green, primaryFixed.Blue, 0.2f);

        for (var i = 0; i < _mnemonicVisuals.Count; i++)
        {
            var (card, radio) = _mnemonicVisuals[i];
            var selected = i == selectedIndex;
            card.Stroke = selected ? primary : outlineVariant;
            card.StrokeThickness = selected ? 2 : 1;
            card.BackgroundColor = selected ? selectedBg : surface;
            card.Shadow = selected
                ? new Shadow
                {
                    Brush = Colors.Black,
                    Offset = new Point(0, 1),
                    Radius = 4,
                    Opacity = 0.06f
                }
                : new Shadow { Opacity = 0f };

            radio.Stroke = selected ? primary : outline;
            radio.StrokeThickness = selected ? 6 : 2;
            radio.BackgroundColor = Colors.Transparent;
        }
    }

    void ApplyRegenerateButtonState(int remainingRegenerations)
    {
        var (text, enabled) = RegenerateActionPresentation.ForRemaining(remainingRegenerations);
        RegenerateButton.Text = text;
        RegenerateButton.IsEnabled = enabled;
    }

    static string FormatQuota(int remaining) =>
        remaining >= int.MaxValue / 2 ? "無限制" : remaining.ToString();

    async void OnPlayClicked(object? sender, EventArgs e)
    {
        var result = _flow.LastResult;
        if (result is null)
        {
            await DisplayAlertAsync("播放", FormalTtsRequest.ErrorEmptySource, "了解");
            return;
        }

        // 只播正式原文；不傳空耳候選。讀音文字已綁在 ReadingLabel，失敗也不清除。
        var play = await _tts.SpeakFormalSourceAsync(result.SourceText, result.SourceLanguage);
        if (!play.Success)
            await DisplayAlertAsync("播放", play.Message ?? FormalTtsMessages.SpeakFailed, "了解");
    }

    async void OnFixLanguageClicked(object? sender, EventArgs e)
    {
        if (OcrSessionRetention.ShouldClearOn(OcrSessionLeaveTarget.WordInput))
            _ocrSession.Clear();
        await Routes.GoAsync(Routes.WordInput);
    }

    async void OnRegenerateClicked(object? sender, EventArgs e)
    {
        var previous = _flow.PendingRequest;
        if (previous is null && _flow.LastResult is not null)
        {
            previous = new AnalyzeRequestDto(
                _flow.LastResult.SourceText,
                string.IsNullOrWhiteSpace(_flow.LastResult.SourceLanguage)
                    ? "auto"
                    : _flow.LastResult.SourceLanguage,
                "zh-TW",
                _flow.LastResult.Mnemonics.FirstOrDefault()?.NotationType ?? "bopomofo");
        }

        if (previous is null)
        {
            await DisplayAlertAsync("提醒", "沒有可重新產生的文字。", "了解");
            return;
        }

        if (_flow.LastResult is not null
            && ReanalyzeGuard.IsRegenerationLimitReached(_flow.LastResult.RemainingRegenerations))
        {
            await DisplayAlertAsync(
                AnalyzeFailureMessages.TitleFor(AnalyzeFailureMessages.RegenerationLimitCode),
                AnalyzeFailureMessages.MessageOrDefault(
                    null,
                    AnalyzeFailureMessages.RegenerationLimitCode),
                "了解");
            return;
        }

        // Prefer detected language so ForceRefresh shares the same regenerate key as API／票 18.
        var language = !string.IsNullOrWhiteSpace(_flow.LastResult?.SourceLanguage)
            ? _flow.LastResult!.SourceLanguage
            : previous.SourceLanguage;

        _flow.PendingRequest = previous with
        {
            ForceRefresh = true,
            SourceLanguage = language
        };
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
