using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class AnalyzingPage : ContentPage
{
    private readonly ISoraeruApiClient _api;
    private readonly IAnalyzeFlowStore _flow;
    private CancellationTokenSource? _cts;
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
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        var request = _flow.PendingRequest;
        if (request is null)
        {
            StatusLabel.Text = "沒有待分析的文字。";
            BusyIndicator.IsRunning = false;
            return;
        }

        WordLabel.Text = request.Text;
        StatusLabel.Text = "連線分析中…";
        BusyIndicator.IsRunning = true;
        MarkStep(0);

        try
        {
            MarkStep(1);
            StatusLabel.Text = "偵測語言與產生空耳…";
            MarkStep(2);

            var result = await _api.AnalyzeWordAsync(request, _cts.Token);
            if (_cts.IsCancellationRequested || !IsLoaded)
                return;

            if (!result.IsSuccess || result.Result is null)
            {
                _flow.LastError = result.Message ?? "分析失敗。";
                BusyIndicator.IsRunning = false;
                StatusLabel.Text = _flow.LastError;
                await DisplayAlertAsync("分析失敗", _flow.LastError, "返回");
                if (!_navigating)
                {
                    _navigating = true;
                    await Routes.BackAsync();
                }

                return;
            }

            MarkStep(3);
            _flow.LastResult = result.Result;
            // Reset force-refresh so a later back-and-forth does not always skip cache.
            _flow.PendingRequest = request with { ForceRefresh = false };
            _flow.ClearError();
            BusyIndicator.IsRunning = false;
            StatusLabel.Text = "完成";

            if (!_navigating)
            {
                _navigating = true;
                await Routes.GoAsync($"../{Routes.AnalysisResult}");
            }
        }
        catch (OperationCanceledException)
        {
            // cancelled by user
        }
        catch (Exception ex)
        {
            if (_cts.IsCancellationRequested || !IsLoaded)
                return;

            _flow.LastError = ex.Message;
            BusyIndicator.IsRunning = false;
            StatusLabel.Text = ex.Message;
            await DisplayAlertAsync("分析失敗", ex.Message, "返回");
            if (!_navigating)
            {
                _navigating = true;
                await Routes.BackAsync();
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    async void OnCancelClicked(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        if (_navigating)
            return;

        _navigating = true;
        await Routes.BackAsync();
    }

    void MarkStep(int completedThrough)
    {
        ApplyStep(StepLanguage, "偵測來源語言", completedThrough >= 1);
        ApplyStep(StepMeaning, "整理詞義及讀音", completedThrough >= 2);
        ApplyStep(StepMnemonics, "產生近似音候選", completedThrough >= 3);
    }

    static void ApplyStep(Label label, string caption, bool done)
    {
        label.Text = (done ? "✓  " : "○  ") + caption;
        if (Application.Current?.Resources.TryGetValue(done ? "Primary" : "OnSurfaceVariant", out var colorObj) == true
            && colorObj is Color color)
        {
            label.TextColor = color;
        }
    }
}
