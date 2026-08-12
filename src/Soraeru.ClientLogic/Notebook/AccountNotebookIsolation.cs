namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Account-switch isolation for local notebook (ADR-0007): never carry A’s cards into B’s session.
/// </summary>
public static class AccountNotebookIsolation
{
    /// <summary>
    /// Clear local notebook when signing in as a different user than the previous session owner.
    /// Same user re-login keeps local data; first login (no previous) keeps whatever is on device
    /// only if we already cleared on logout — still clear when previous owner is known and differs.
    /// </summary>
    public static bool ShouldClearLocalOnSignIn(Guid? previousUserId, Guid nextUserId)
    {
        if (nextUserId == Guid.Empty)
            return false;

        if (previousUserId is null || previousUserId == Guid.Empty)
            return false;

        return previousUserId.Value != nextUserId;
    }
}
