using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

/// <summary>
/// Logout clears session only; same-account sign-in must still list local owned cards
/// immediately (UI refresh is separate — this locks the SoT / owner-filter contract).
/// </summary>
public sealed class PostLoginNotebookListContractTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-01T10:00:00Z");

    [Fact]
    public async Task After_explicit_logout_then_same_account_sign_in_ListAsync_returns_owned_cards()
    {
        var store = new InMemoryLocalWordCardStore();
        var cardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await store.SaveAllAsync(
        [
            new LocalWordCard(
                cardId,
                UserA,
                "hello",
                "hello",
                "en",
                "你好",
                "heh-loh",
                "哈囉",
                T0,
                T0,
                null)
        ]);

        var session = LocalSession.SignedIn(UserA);
        var notebook = new LocalNotebookService(store, () => session);

        // Explicit logout: session cleared, local SoT kept (DecideLogout).
        session = LocalSession.Anonymous();

        // Login page: previous owner unknown after ClearAsync.
        await SignInNotebookIsolation.ApplyAsync(notebook, previousUserId: null, UserA);
        session = LocalSession.SignedIn(UserA);

        var list = await notebook.ListAsync();
        list.Count.ShouldBe(1);
        list[0].Id.ShouldBe(cardId);
    }
}
