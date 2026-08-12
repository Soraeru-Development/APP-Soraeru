namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Apply account-switch isolation against the local SoT before replacing the session.
/// </summary>
public static class SignInNotebookIsolation
{
    public static async Task ApplyAsync(
        LocalNotebookService notebook,
        Guid? previousUserId,
        Guid nextUserId,
        CancellationToken cancellationToken = default)
    {
        if (AccountNotebookIsolation.ShouldClearLocalOnSignIn(previousUserId, nextUserId))
        {
            await notebook.ClearLocalNotebookAsync(cancellationToken);
            return;
        }

        await notebook.EnsureOwnerIsolationAsync(nextUserId, cancellationToken);
    }
}
