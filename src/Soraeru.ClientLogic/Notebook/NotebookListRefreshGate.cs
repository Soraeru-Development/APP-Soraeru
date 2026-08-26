namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Signals that the notebook list UI should reload (e.g. after login sync).
/// Shell tabs may skip <c>OnAppearing</c>; callers bump this so the list page can catch up.
/// </summary>
public sealed class NotebookListRefreshGate
{
    private int _version;

    public int Version => Volatile.Read(ref _version);

    public void NotifyDataMayHaveChanged() => Interlocked.Increment(ref _version);

    public bool NeedsReload(int lastLoadedVersion) => lastLoadedVersion != Version;
}
