using Soraeru.ClientLogic.Analyze;
using Soraeru.Languages;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class AnalyzingPage : ContentPage
{
    enum StepVisual
    {
        Pending,
        Active,
        Done
    }

    private readonly ISoraeruApiClient _api;
    private readonly IAnalyzeFlowStore _flow;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _spinCts;
    private CancellationTokenSource? _pulseCts;
    private bool _navigating;

    public AnalyzingPage(ISoraeruApiClient api, IAnalyzeFlowStore flow)
    {
        InitializeComponent();
        _api = api;
        _flow = flow;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _navigating = false;
        _ = AmbientBackground.StartAsync();
        StartTitlePulse();
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        var request = _flow.PendingRequest;
        if (request is null)
        {
            StatusLabel.IsVisible = true;
            StatusLabel.Text = "沒有待分析的文字。";
            BusyIndicator.IsRunning = false;
            ApplyChecklist(0, languageSubtitle: string.Empty);
            return;
        }

        WordLabel.Text = request.Text;
        StatusLabel.IsVisible = false;
        BusyIndicator.IsRunning = true;
        ApplyChecklist(0, languageSubtitle: string.Empty);

        try
        {
            var analyzeTask = _api.AnalyzeWordAsync(request, ct);
            await AdvancePipelineWhileWaitingAsync(analyzeTask, request, ct);

            var result = await analyzeTask;
            if (ct.IsCancellationRequested || !IsLoaded)
                return;

            if (!result.IsSuccess || result.Result is null)
            {
                var kindKey = result.Failure switch
                {
                    AnalyzeFailureKind.QuotaExceeded => "QuotaExceeded",
                    AnalyzeFailureKind.RegenerationLimit => "RegenerationLimit",
                    AnalyzeFailureKind.AnalyzeFailed => "AnalyzeFailed",
                    AnalyzeFailureKind.Network => "Network",
                    _ => null
                };
                var title = AnalyzeFailureMessages.TitleFor(code: null, fallbackKind: kindKey);
                var message = AnalyzeFailureMessages.MessageOrDefault(result.Message, code: null, fallbackKind: kindKey);
                _flow.LastError = message;
                BusyIndicator.IsRunning = false;
                StatusLabel.IsVisible = true;
                StatusLabel.Text = message;
                await DisplayAlertAsync(title, message, "返回");
                if (!_navigating)
                {
                    _navigating = true;
                    await Routes.BackAsync();
                }

                return;
            }

            var languageSubtitle = ResolveLanguageSubtitle(request.SourceLanguage, result.Result);
            ApplyChecklist(3, languageSubtitle);
            _flow.LastResult = result.Result;
            // Reset force-refresh so a later back-and-forth does not always skip cache.
            _flow.PendingRequest = request with { ForceRefresh = false };
            _flow.ClearError();
            BusyIndicator.IsRunning = false;

            if (!_navigating)
            {
                _navigating = true;
                // Pop Analyzing first so OcrSelect stays under Result; `../Result` can drop OcrSelect.
                await Routes.GoAsync("..", animate: false);
                await Routes.GoAsync(Routes.AnalysisResult);
            }
        }
        catch (OperationCanceledException)
        {
            // cancelled by user
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested || !IsLoaded)
                return;

            _flow.LastError = ex.Message;
            BusyIndicator.IsRunning = false;
            StatusLabel.IsVisible = true;
            StatusLabel.Text = ex.Message;
            await DisplayAlertAsync("分析失敗", ex.Message, "返回");
            if (!_navigating)
            {
                _navigating = true;
                await Routes.BackAsync();
            }
        }
        finally
        {
            StopActiveSpin();
        }
    }

    protected override void OnDisappearing()
    {
        StopTitlePulse();
        StopActiveSpin();
        AmbientBackground.Stop();
        _cts?.Cancel();
        base.OnDisappearing();
    }

    void StartTitlePulse()
    {
        StopTitlePulse();
        _pulseCts = new CancellationTokenSource();
        var token = _pulseCts.Token;
        _ = PulseTitleAsync(token);
    }

    void StopTitlePulse()
    {
        _pulseCts?.Cancel();
        _pulseCts?.Dispose();
        _pulseCts = null;
        try
        {
            TitleLabel.Opacity = 1;
        }
        catch (ObjectDisposedException)
        {
            // page torn down
        }
    }

    /// <summary>Stitch L09 pulse-text: opacity 1 ↔ 0.5 over 2s.</summary>
    async Task PulseTitleAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await TitleLabel.FadeToAsync(0.5, 1000, Easing.CubicInOut);
                if (token.IsCancellationRequested)
                    break;
                await TitleLabel.FadeToAsync(1.0, 1000, Easing.CubicInOut);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (ObjectDisposedException)
        {
            // page torn down
        }
    }

    async void OnCancelClicked(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        if (_navigating)
            return;

        _navigating = true;
        await Routes.BackAsync();
    }

    /// <summary>
    /// Advances checklist stages while the network call is in flight.
    /// Completes early if the API finishes sooner than the staged delays.
    /// </summary>
    async Task AdvancePipelineWhileWaitingAsync(
        Task analyzeTask,
        AnalyzeRequestDto request,
        CancellationToken ct)
    {
        var knownLanguage = SourceLanguageCatalog.FormatAnalyzingSubtitle(request.SourceLanguage);

        ApplyChecklist(0, languageSubtitle: string.Empty);
        if (await WaitOrDoneAsync(analyzeTask, 450, ct))
            return;

        ApplyChecklist(1, string.IsNullOrEmpty(knownLanguage) ? "自動偵測" : knownLanguage);
        if (await WaitOrDoneAsync(analyzeTask, 500, ct))
            return;

        ApplyChecklist(2, string.IsNullOrEmpty(knownLanguage) ? "自動偵測" : knownLanguage);
        // Remain on step 3 (active) until analyzeTask completes.
    }

    static async Task<bool> WaitOrDoneAsync(Task analyzeTask, int delayMs, CancellationToken ct)
    {
        if (analyzeTask.IsCompleted)
            return true;

        var delay = Task.Delay(delayMs, ct);
        var finished = await Task.WhenAny(analyzeTask, delay);
        return ReferenceEquals(finished, analyzeTask) || analyzeTask.IsCompleted;
    }

    void ApplyChecklist(int completedThrough, string languageSubtitle)
    {
        var step1 = completedThrough >= 1
            ? StepVisual.Done
            : completedThrough == 0
                ? StepVisual.Active
                : StepVisual.Pending;
        var step2 = completedThrough >= 2
            ? StepVisual.Done
            : completedThrough == 1
                ? StepVisual.Active
                : StepVisual.Pending;
        var step3 = completedThrough >= 3
            ? StepVisual.Done
            : completedThrough == 2
                ? StepVisual.Active
                : StepVisual.Pending;

        ApplyStep(
            Step1Card, Step1Accent, Step1IconCircle, Step1Icon, Step1Title, Step1Subtitle,
            "偵測來源語言",
            step1 switch
            {
                StepVisual.Done => string.IsNullOrWhiteSpace(languageSubtitle) ? "完成" : languageSubtitle,
                StepVisual.Active => "AI 正在偵測來源語言...",
                _ => string.Empty
            },
            step1);

        ApplyStep(
            Step2Card, Step2Accent, Step2IconCircle, Step2Icon, Step2Title, Step2Subtitle,
            "整理詞義及讀音",
            step2 switch
            {
                StepVisual.Done => "完成",
                StepVisual.Active => "正在整理詞義及讀音...",
                _ => string.Empty
            },
            step2);

        ApplyStep(
            Step3Card, Step3Accent, Step3IconCircle, Step3Icon, Step3Title, Step3Subtitle,
            "產生近似音候選",
            step3 switch
            {
                StepVisual.Done => "完成",
                StepVisual.Active => "AI 正在生成諧音記憶法...",
                _ => string.Empty
            },
            step3);

        if (step1 == StepVisual.Active || step2 == StepVisual.Active || step3 == StepVisual.Active)
        {
            var spinning = step1 == StepVisual.Active ? Step1Icon
                : step2 == StepVisual.Active ? Step2Icon
                : Step3Icon;
            StartActiveSpin(spinning);
        }
        else
        {
            StopActiveSpin();
        }
    }

    void ApplyStep(
        Border card,
        BoxView accent,
        Border iconCircle,
        Label icon,
        Label title,
        Label subtitle,
        string titleText,
        string subtitleText,
        StepVisual state)
    {
        title.Text = titleText;
        subtitle.Text = subtitleText;
        subtitle.IsVisible = !string.IsNullOrEmpty(subtitleText);

        var primary = ResolveColor("Primary", Colors.Teal);
        var onSurface = ResolveColor("OnSurface", Colors.Black);
        var onSurfaceVariant = ResolveColor("OnSurfaceVariant", Colors.Gray);
        var lowest = ResolveColor("SurfaceContainerLowest", Colors.White);
        var outlineVariant = ResolveColor("OutlineVariant", Colors.LightGray);

        switch (state)
        {
            case StepVisual.Done:
                card.BackgroundColor = lowest;
                card.Stroke = outlineVariant;
                card.Opacity = 0.85;
                accent.IsVisible = false;
                iconCircle.BackgroundColor = Color.FromArgb("#33006684");
                iconCircle.StrokeThickness = 0;
                icon.Text = "✓";
                icon.TextColor = primary;
                title.TextColor = onSurface;
                subtitle.TextColor = onSurfaceVariant;
                break;

            case StepVisual.Active:
                card.BackgroundColor = Color.FromArgb("#0D004D64");
                card.Stroke = Color.FromArgb("#33004D64");
                card.Opacity = 1;
                accent.IsVisible = true;
                iconCircle.BackgroundColor = Colors.Transparent;
                iconCircle.Stroke = primary;
                iconCircle.StrokeThickness = 2;
                icon.Text = "↻";
                icon.TextColor = primary;
                title.TextColor = primary;
                subtitle.TextColor = onSurfaceVariant;
                break;

            default:
                card.BackgroundColor = lowest;
                card.Stroke = outlineVariant;
                card.Opacity = 0.7;
                accent.IsVisible = false;
                iconCircle.BackgroundColor = Color.FromArgb("#14004D64");
                iconCircle.StrokeThickness = 0;
                icon.Text = "○";
                icon.TextColor = onSurfaceVariant;
                title.TextColor = onSurfaceVariant;
                subtitle.TextColor = onSurfaceVariant;
                break;
        }
    }

    void StartActiveSpin(Label icon)
    {
        StopActiveSpin();
        _spinCts = new CancellationTokenSource();
        var token = _spinCts.Token;
        _ = SpinIconAsync(icon, token);
    }

    void StopActiveSpin()
    {
        _spinCts?.Cancel();
        _spinCts?.Dispose();
        _spinCts = null;
    }

    static async Task SpinIconAsync(Label icon, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await icon.RotateToAsync(360, 1400, Easing.Linear);
                icon.Rotation = 0;
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (ObjectDisposedException)
        {
            // page torn down
        }
    }

    static string ResolveLanguageSubtitle(string requestLanguage, AnalyzeResultDto result)
    {
        if (!string.IsNullOrWhiteSpace(result.LanguageDisplayName))
        {
            var display = result.LanguageDisplayName.Trim();
            var code = string.IsNullOrWhiteSpace(result.SourceLanguage)
                ? string.Empty
                : result.SourceLanguage.Trim();
            if (!string.IsNullOrEmpty(code)
                && !display.Contains(code, StringComparison.OrdinalIgnoreCase)
                && !display.Contains('('))
            {
                return $"{display} ({SourceLanguageCatalog.Resolve(code).EnglishName})";
            }

            return display;
        }

        var fromRequest = SourceLanguageCatalog.FormatAnalyzingSubtitle(requestLanguage);
        if (!string.IsNullOrEmpty(fromRequest))
            return fromRequest;

        return SourceLanguageCatalog.FormatAnalyzingSubtitle(result.SourceLanguage);
    }

    static Color ResolveColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var obj) == true && obj is Color color)
            return color;
        return fallback;
    }
}
