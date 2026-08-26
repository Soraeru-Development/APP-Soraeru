using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class SignInNotebookIsolationTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-01T10:00:00Z");

    [Fact]
    public async Task ApplyAsync_when_switching_accounts_keeps_previous_users_cards_in_store()
    {
        var store = new InMemoryLocalWordCardStore();
        var cardAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await store.SaveAllAsync(
        [
            new LocalWordCard(
                cardAId,
                UserA,
                "a",
                "a",
                "en",
                "甲",
                "a",
                "空耳A",
                T0,
                T0,
                null)
        ]);

        var notebook = new LocalNotebookService(store, () => LocalSession.SignedIn(UserB));
        await SignInNotebookIsolation.ApplyAsync(notebook, UserA, UserB);

        var raw = await store.LoadAllAsync();
        raw.Count.ShouldBe(1);
        raw[0].Id.ShouldBe(cardAId);
        raw[0].OwnerUserId.ShouldBe(UserA);
        (await notebook.ListAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_when_same_account_keeps_owned_cards()
    {
        var store = new InMemoryLocalWordCardStore();
        var cardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await store.SaveAllAsync(
        [
            new LocalWordCard(cardId, UserA, "a", "a", "en", "甲", "a", "空耳A", T0, T0, null)
        ]);

        var notebook = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        await SignInNotebookIsolation.ApplyAsync(notebook, UserA, UserA);

        var raw = await store.LoadAllAsync();
        raw.Count.ShouldBe(1);
        raw[0].Id.ShouldBe(cardId);
    }
}
