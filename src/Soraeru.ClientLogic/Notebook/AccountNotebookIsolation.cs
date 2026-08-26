namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Account-switch isolation for local notebook (ADR-0007): list by OwnerUserId;
/// never wipe other users' on-device rows when switching accounts.
/// </summary>
public static class AccountNotebookIsolation
{
    /// <summary>
    /// Always false: multi-user cards coexist in the on-device DB. Switching accounts only
    /// changes the session filter; logout / 401 also keep other owners' rows.
    /// Delete-account clears the current owner's rows via <c>ClearLocalNotebookAsync</c>.
    /// </summary>
    public static bool ShouldClearLocalOnSignIn(Guid? previousUserId, Guid nextUserId)
    {
        _ = previousUserId;
        _ = nextUserId;
        return false;
    }
}
