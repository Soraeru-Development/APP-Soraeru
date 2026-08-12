using Soraeru.ClientLogic.Analyze;
using Soraeru.ClientLogic.Notebook;
using Soraeru.ClientLogic.Tts;
using Soraeru.Languages;
using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

[QueryProperty(nameof(CardId), "cardId")]
public partial class NotebookDetailPage : ContentPage
{
    private readonly LocalNotebookService _notebook;
    private readonly IFormalTtsService _tts;
    private readonly IAnalyzeFlowStore _flow;
    private readonly ISoraeruApiClient _api;
    private Guid? _cardId;
    private LocalWordCard? _card;

    public NotebookDetailPage(
        LocalNotebookService notebook,
        IFormalTtsService tts,
        IAnalyzeFlowStore flow,
        ISoraeruApiClient api)
    {
        InitializeComponent();
        _notebook = notebook;
        _tts = tts;
        _flow = flow;
        _api = api;

        WordCardBorder.Shadow = new Shadow
        {
            Brush = Colors.Black,
            Offset = new Point(0, 2),
            Radius = 8,
            Opacity = 0.05f
        };
        PlayChrome.Shadow = new Shadow
        {
            Brush = Colors.Black,
            Offset = new Point(0, 1),
            Radius = 4,
            Opacity = 0.08f
        };
    }

    public string CardId
    {
        set
        {
            if (Guid.TryParse(value, out var id))
                _cardId = id;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        _card = null;
        if (_cardId is null)
        {
            SourceTextLabel.Text = "找不到單字卡";
            LanguagePillLabel.Text = string.Empty;
            ReadingLabel.Text = string.Empty;
            MeaningLabel.Text = string.Empty;
            MnemonicLabel.Text = string.Empty;
            SetWriteControls(canWrite: false);
            ReanalyzeButton.IsEnabled = false;
            return;
        }

        try
        {
            var card = await _notebook.GetAsync(_cardId.Value);
            if (card is null)
            {
                SourceTextLabel.Text = "找不到單字卡";
                LanguagePillLabel.Text = string.Empty;
                ReadingLabel.Text = string.Empty;
                MeaningLabel.Text = string.Empty;
                MnemonicLabel.Text = string.Empty;
                SetWriteControls(canWrite: false);
                ReanalyzeButton.IsEnabled = false;
                return;
            }

            _card = card;
            var lang = SourceLanguageCatalog.Resolve(card.DetectedLanguage);
            LanguagePillLabel.Text = lang.EnglishName;
            SourceTextLabel.Text = card.SourceText;
            ReadingLabel.Text = card.Pronunciation;
            ReadingLabel.IsVisible = !string.IsNullOrWhiteSpace(card.Pronunciation);
            MeaningLabel.Text = card.MeaningZh;
            ApplyMnemonicDisplay(card.SelectedMnemonic);
            SetWriteControls(await _notebook.CanWriteAsync());
            await UpdateReanalyzeButtonStateAsync(card);
        }
        catch (Exception ex)
        {
            _card = null;
            SourceTextLabel.Text = "讀取失敗";
            LanguagePillLabel.Text = string.Empty;
            ReadingLabel.Text = string.Empty;
            MeaningLabel.Text = ex.Message;
            MnemonicLabel.Text = string.Empty;
            SetWriteControls(canWrite: false);
            ReanalyzeButton.IsEnabled = false;
        }
    }

    async Task UpdateReanalyzeButtonStateAsync(LocalWordCard card)
    {
        if (!await _notebook.CanWriteAsync())
        {
            ReanalyzeButton.IsEnabled = false;
            return;
        }

        if (!ReanalyzeGuard.TryResolveSourceLanguage(
                card.DetectedLanguage,
                _flow.LastResult?.SourceLanguage,
                out _))
        {
            ReanalyzeButton.IsEnabled = false;
            return;
        }

        var remaining = await TryGetKnownRemainingRegenerationsAsync(card);
        ReanalyzeButton.IsEnabled = remaining is null || !ReanalyzeGuard.IsRegenerationLimitReached(remaining.Value);
    }

    void SetWriteControls(bool canWrite)
    {
        DeleteButton.IsEnabled = canWrite;
        EditMnemonicButton.IsVisible = canWrite;
        EditMnemonicButton.IsEnabled = canWrite;
    }

    void ApplyMnemonicDisplay(string? mnemonic)
    {
        MnemonicLabel.Text = string.IsNullOrWhiteSpace(mnemonic)
            ? "（尚未填寫近似音）"
            : mnemonic;
    }

    async void OnPlayClicked(object? sender, EventArgs e)
    {
        var card = _card;
        if (card is null)
        {
            await DisplayAlertAsync("播放", FormalTtsRequest.ErrorEmptySource, "了解");
            return;
        }

        // 只播 SourceText（正式原文），不播 SelectedMnemonic。
        var play = await _tts.SpeakFormalSourceAsync(card.SourceText, card.DetectedLanguage);
        if (!play.Success)
            await DisplayAlertAsync("播放", play.Message ?? FormalTtsMessages.SpeakFailed, "了解");
    }

    async void OnEditMnemonicClicked(object? sender, EventArgs e)
    {
        if (_cardId is null || _card is null)
            return;

        if (!await _notebook.CanWriteAsync())
        {
            await DisplayAlertAsync("需要登入", "未登入時單字本為唯讀，無法編修個人空耳。", "了解");
            SetWriteControls(canWrite: false);
            return;
        }

        var current = _card.SelectedMnemonic ?? string.Empty;
        var input = await DisplayPromptAsync(
            "編修個人空耳",
            "修改後會寫入本機單字本（不呼叫 AI 分析）。",
            accept: "保存",
            cancel: "取消",
            placeholder: "輸入你的近似音",
            maxLength: 500,
            keyboard: Keyboard.Default,
            initialValue: current);

        if (input is null)
            return;

        var result = await _notebook.UpdateSelectedMnemonicAsync(_cardId.Value, input);
        if (!result.IsSuccess || result.Value is null)
        {
            await DisplayAlertAsync("保存失敗", result.Message ?? "無法更新個人空耳。", "了解");
            return;
        }

        _card = result.Value;
        ApplyMnemonicDisplay(result.Value.SelectedMnemonic);
    }

    async void OnReanalyzeClicked(object? sender, EventArgs e)
    {
        var card = _card;
        if (card is null)
            return;

        var authenticated = await _notebook.CanWriteAsync();
        var decision = AnalyzeEntryGate.DecideReanalyze(authenticated);
        if (decision.Kind == AnalyzeEntryKind.RequireLogin)
        {
            await DisplayAlertAsync("需要登入", "登入後才能重新分析（會計入額度）。", "了解");
            await Routes.GoAsync(Routes.Login);
            return;
        }

        if (!ReanalyzeGuard.TryResolveSourceLanguage(
                card.DetectedLanguage,
                _flow.LastResult?.SourceLanguage,
                out var sourceLanguage))
        {
            await DisplayAlertAsync("無法重新分析", "此卡缺少可用語言資訊，請改從輸入頁重新分析。", "了解");
            return;
        }

        var remaining = await TryGetKnownRemainingRegenerationsAsync(card);
        if (remaining is not null && ReanalyzeGuard.IsRegenerationLimitReached(remaining.Value))
        {
            await ShowRegenerationLimitAlertAsync();
            return;
        }

        var notation = ResolveNotationPreference();
        // Shared contract with ticket 09: ForceRefresh counts regenerations + daily quota.
        _flow.PendingRequest = new AnalyzeRequestDto(
            card.SourceText,
            sourceLanguage,
            MemoryLanguage: "zh-TW",
            NotationPreference: notation,
            ForceRefresh: decision.ForceRefresh);
        _flow.ClearError();

        await Routes.GoAsync(Routes.Analyzing);
    }

    async Task<int?> TryGetKnownRemainingRegenerationsAsync(LocalWordCard card)
    {
        var last = _flow.LastResult;
        if (last is not null
            && ReanalyzeGuard.FlowResultMatchesCard(
                card.NormalizedText,
                card.DetectedLanguage,
                last.NormalizedText,
                last.SourceLanguage))
        {
            return last.RemainingRegenerations;
        }

        if (!ReanalyzeGuard.TryResolveSourceLanguage(
                card.DetectedLanguage,
                last?.SourceLanguage,
                out var sourceLanguage))
        {
            return null;
        }

        var peek = await _api.AnalyzeWordAsync(new AnalyzeRequestDto(
            card.SourceText,
            sourceLanguage,
            MemoryLanguage: "zh-TW",
            NotationPreference: ResolveNotationPreference(),
            ForceRefresh: false));

        if (!peek.IsSuccess || peek.Result is null || !peek.Result.Cached)
            return null;

        return peek.Result.RemainingRegenerations;
    }

    string ResolveNotationPreference()
    {
        var last = _flow.LastResult;
        var card = _card;
        if (card is not null
            && last is not null
            && ReanalyzeGuard.FlowResultMatchesCard(
                card.NormalizedText,
                card.DetectedLanguage,
                last.NormalizedText,
                last.SourceLanguage))
        {
            return last.Mnemonics.FirstOrDefault()?.NotationType ?? "bopomofo";
        }

        return "bopomofo";
    }

    static Task ShowRegenerationLimitAlertAsync(ContentPage page) =>
        page.DisplayAlertAsync(
            AnalyzeFailureMessages.TitleFor(AnalyzeFailureMessages.RegenerationLimitCode),
            AnalyzeFailureMessages.MessageOrDefault(
                null,
                AnalyzeFailureMessages.RegenerationLimitCode),
            "了解");

    Task ShowRegenerationLimitAlertAsync() => ShowRegenerationLimitAlertAsync(this);

    async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_cardId is null)
            return;

        if (!await _notebook.CanWriteAsync())
        {
            await DisplayAlertAsync("需要登入", "未登入時單字本為唯讀，無法刪除。", "了解");
            return;
        }

        var ok = await DisplayAlertAsync("刪除單字卡", "確定要刪除此單字卡？", "刪除", "取消");
        if (!ok)
            return;

        var result = await _notebook.DeleteAsync(_cardId.Value);
        if (!result.IsSuccess)
        {
            await DisplayAlertAsync("刪除失敗", result.Message ?? "無法刪除。", "了解");
            return;
        }

        await Routes.BackAsync();
    }
}
