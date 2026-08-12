namespace Soraeru.ClientLogic.Notebook;

public enum AnalyzeEntryKind
{
    OpenLocalDetail,
    ProceedToAnalyze,
    RequireLogin
}

/// <summary>
/// Pure entry decision for 本機短路／重新分析（票 18）. Does not call cloud or analyze API.
/// </summary>
public sealed record AnalyzeEntryDecision(
    AnalyzeEntryKind Kind,
    Guid? CardId = null,
    bool ForceRefresh = false,
    bool CountsTowardQuota = false)
{
    public bool CallsAnalyze => Kind == AnalyzeEntryKind.ProceedToAnalyze;
}

public static class AnalyzeEntryGate
{
    public static AnalyzeEntryDecision DecideLookup(LocalWordCard? match, bool isAuthenticated)
    {
        if (match is not null)
        {
            return new AnalyzeEntryDecision(
                AnalyzeEntryKind.OpenLocalDetail,
                CardId: match.Id,
                ForceRefresh: false,
                CountsTowardQuota: false);
        }

        if (!isAuthenticated)
        {
            return new AnalyzeEntryDecision(
                AnalyzeEntryKind.RequireLogin,
                ForceRefresh: false,
                CountsTowardQuota: false);
        }

        return new AnalyzeEntryDecision(
            AnalyzeEntryKind.ProceedToAnalyze,
            ForceRefresh: false,
            CountsTowardQuota: true);
    }

    public static AnalyzeEntryDecision DecideReanalyze(bool isAuthenticated)
    {
        if (!isAuthenticated)
        {
            return new AnalyzeEntryDecision(
                AnalyzeEntryKind.RequireLogin,
                ForceRefresh: false,
                CountsTowardQuota: false);
        }

        return new AnalyzeEntryDecision(
            AnalyzeEntryKind.ProceedToAnalyze,
            ForceRefresh: true,
            CountsTowardQuota: true);
    }
}
