namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Push/pull orchestrator: merge local SoT with cloud mirror (ADR-0007).
/// </summary>
public sealed class NotebookSyncCoordinator
{
    private readonly ILocalWordCardStore _store;
    private readonly ICloudWordCardMirror _mirror;
    private readonly Func<CancellationToken, Task<LocalSession>> _session;

    public NotebookSyncCoordinator(
        ILocalWordCardStore store,
        ICloudWordCardMirror mirror,
        Func<CancellationToken, Task<LocalSession>> session)
    {
        _store = store;
        _mirror = mirror;
        _session = session;
    }

    public NotebookSyncCoordinator(
        ILocalWordCardStore store,
        ICloudWordCardMirror mirror,
        Func<LocalSession> session)
        : this(store, mirror, _ => Task.FromResult(session()))
    {
    }

    public async Task<NotebookSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var session = await _session(cancellationToken);
        if (!session.IsAuthenticated || session.UserId is not { } userId || userId == Guid.Empty)
            return new NotebookSyncResult(NotebookSyncStatus.SkippedNotAuthenticated);

        var pull = await _mirror.PullAsync(cancellationToken);
        if (!pull.IsSuccess || pull.Cards is null)
            return new NotebookSyncResult(NotebookSyncStatus.SkippedOffline, pull.ErrorCode);

        var allLocal = (await _store.LoadAllAsync(cancellationToken)).ToList();
        var ownerLocal = allLocal.Where(c => c.OwnerUserId == userId).ToList();
        var others = allLocal.Where(c => c.OwnerUserId != userId).ToList();

        // Never merge foreign-owner mirror rows into this account.
        var remote = pull.Cards.Where(c => c.OwnerUserId == userId).ToList();

        var mergedOwner = WordCardSyncMerger.Merge(ownerLocal, remote)
            .Select(c => c with { OwnerUserId = userId })
            .ToList();

        var nextLocal = others.Concat(mergedOwner).ToList();
        await _store.SaveAllAsync(nextLocal, cancellationToken);

        var push = await _mirror.PushAsync(mergedOwner, cancellationToken);
        if (!push.IsSuccess)
            return new NotebookSyncResult(NotebookSyncStatus.Failed, push.ErrorCode);

        return new NotebookSyncResult(NotebookSyncStatus.Completed);
    }
}
