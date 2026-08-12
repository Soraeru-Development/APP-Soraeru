using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class SignInNotebookIsolationTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-01T10:00:00Z");

    [Fact]
    public async Task ApplyAsync_when_switching_accounts_clears_entire_local_notebook()
    {
        var store = new InMemoryLocalWordCardStore();
        await store.SaveAllAsync(
        [
            new LocalWordCard(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
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

        (await store.LoadAllAsync()).ShouldBeEmpty();
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
