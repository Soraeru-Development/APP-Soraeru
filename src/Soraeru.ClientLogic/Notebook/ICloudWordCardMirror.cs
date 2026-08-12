namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Cloud notebook mirror boundary for optional sync (ticket 14; real API = ticket 15).
/// </summary>
public interface ICloudWordCardMirror
{
    Task<CloudMirrorPullResult> PullAsync(CancellationToken cancellationToken = default);

    Task<CloudMirrorPushResult> PushAsync(
        IReadOnlyList<LocalWordCard> cards,
        CancellationToken cancellationToken = default);
}

public sealed record CloudMirrorPullResult(
    bool IsSuccess,
    IReadOnlyList<LocalWordCard>? Cards,
    string? ErrorCode)
{
    public static CloudMirrorPullResult Success(IReadOnlyList<LocalWordCard> cards) =>
        new(true, cards, null);

    public static CloudMirrorPullResult Failure(string errorCode) =>
        new(false, null, errorCode);
}

public sealed record CloudMirrorPushResult(bool IsSuccess, string? ErrorCode)
{
    public static CloudMirrorPushResult Success() => new(true, null);

    public static CloudMirrorPushResult Failure(string errorCode) => new(false, errorCode);
}

public enum NotebookSyncStatus
{
    Completed,
    SkippedNotAuthenticated,
    SkippedOffline,
    Failed
}

public sealed record NotebookSyncResult(NotebookSyncStatus Status, string? Message = null);
