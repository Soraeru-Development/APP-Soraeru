namespace Soraeru.Services.Interfaces;

/// <summary>
/// Holds pending analyze request / result across L06 → L09 → L10.
/// </summary>
public interface IAnalyzeFlowStore
{
    AnalyzeRequestDto? PendingRequest { get; set; }

    AnalyzeResultDto? LastResult { get; set; }

    string? LastError { get; set; }

    void ClearError();
}

public sealed class AnalyzeFlowStore : IAnalyzeFlowStore
{
    public AnalyzeRequestDto? PendingRequest { get; set; }

    public AnalyzeResultDto? LastResult { get; set; }

    public string? LastError { get; set; }

    public void ClearError() => LastError = null;
}
