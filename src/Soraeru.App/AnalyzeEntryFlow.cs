using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru;

/// <summary>
/// Shared 本機短路＋登入門＋分析導航（票 18）for WordInput／OCR entry pages.
/// </summary>
public static class AnalyzeEntryFlow
{
    public static async Task RouteLookupAsync(
        ContentPage page,
        LocalNotebookService notebook,
        IAnalyzeFlowStore flow,
        string text,
        string sourceLanguage,
        string memoryLanguage,
        string notationPreference)
    {
        var match = await notebook.FindActiveByLookupKeyAsync(text, sourceLanguage);
        var authenticated = await notebook.CanWriteAsync();
        var decision = AnalyzeEntryGate.DecideLookup(match, authenticated);

        if (decision.Kind == AnalyzeEntryKind.OpenLocalDetail && decision.CardId is { } cardId)
        {
            await Routes.GoAsync($"{Routes.NotebookDetail}?cardId={cardId:D}");
            return;
        }

        if (decision.Kind == AnalyzeEntryKind.RequireLogin)
        {
            await page.DisplayAlertAsync("需要登入", "登入後才能分析新單字。", "了解");
            await Routes.GoAsync(Routes.Login);
            return;
        }

        flow.PendingRequest = new AnalyzeRequestDto(
            text,
            sourceLanguage,
            memoryLanguage,
            notationPreference,
            ForceRefresh: decision.ForceRefresh);
        flow.ClearError();

        await Routes.GoAsync(Routes.Analyzing);
    }
}
