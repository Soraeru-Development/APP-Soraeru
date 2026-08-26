using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class AccountNotebookIsolationTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void ShouldClearLocalOnSignIn_when_switching_accounts_does_not_clear_store()
    {
        // Multi-user coexistence: switch only changes session filter; never wipe other owners.
        AccountNotebookIsolation.ShouldClearLocalOnSignIn(UserA, UserB).ShouldBeFalse();
    }

    [Fact]
    public void ShouldClearLocalOnSignIn_when_same_account_keeps_local()
    {
        AccountNotebookIsolation.ShouldClearLocalOnSignIn(UserA, UserA).ShouldBeFalse();
    }

    [Fact]
    public void ShouldClearLocalOnSignIn_when_no_previous_user_does_not_clear()
    {
        AccountNotebookIsolation.ShouldClearLocalOnSignIn(null, UserB).ShouldBeFalse();
    }
}
