namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Apply account-switch isolation against the local SoT before replacing the session.
/// Multi-user coexistence: never wipe previous owners' rows; list filters by current user.
/// </summary>
public static class SignInNotebookIsolation
{
    public static Task ApplyAsync(
        LocalNotebookService notebook,
        Guid? previousUserId,
        Guid nextUserId,
        CancellationToken cancellationToken = default)
    {
        // Kept for call-site compatibility; isolation is session filter + OwnerUserId, not wipe.
        _ = notebook;
        _ = previousUserId;
        _ = nextUserId;
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}
