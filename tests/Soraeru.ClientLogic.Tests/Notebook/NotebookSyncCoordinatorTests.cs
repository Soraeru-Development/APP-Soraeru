using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class NotebookSyncCoordinatorTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid CardLocal = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CardRemote = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CardConflict = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-01T10:00:00Z");
    private static readonly DateTimeOffset T1 = DateTimeOffset.Parse("2026-08-01T11:00:00Z");
    private static readonly DateTimeOffset T2 = DateTimeOffset.Parse("2026-08-01T12:00:00Z");

    [Fact]
    public async Task Sync_when_authenticated_online_pulls_remote_only_card_into_local_and_pushes_union()
    {
        var store = new InMemoryLocalWordCardStore();
        await store.SaveAllAsync([Card(CardLocal, UserA, "local-only", T1)]);

        var mirror = new InMemoryCloudWordCardMirror();
        mirror.Seed([Card(CardRemote, UserA, "remote-only", T1)]);

        var sut = new NotebookSyncCoordinator(
            store,
            mirror,
            () => LocalSession.SignedIn(UserA));

        var result = await sut.SyncAsync();

        result.Status.ShouldBe(NotebookSyncStatus.Completed);
        var local = await store.LoadAllAsync();
        local.Count.ShouldBe(2);
        local.ShouldContain(c => c.Id == CardLocal);
        local.ShouldContain(c => c.Id == CardRemote);

        mirror.Cards.Count.ShouldBe(2);
        mirror.Cards.ShouldContain(c => c.Id == CardLocal);
        mirror.Cards.ShouldContain(c => c.Id == CardRemote);
    }

    [Fact]
    public async Task Sync_resolves_conflict_with_whole_card_LWW()
    {
        var store = new InMemoryLocalWordCardStore();
        await store.SaveAllAsync([Card(CardConflict, UserA, "local-old", T1)]);

        var mirror = new InMemoryCloudWordCardMirror();
        mirror.Seed([Card(CardConflict, UserA, "remote-new", T2)]);

        var sut = new NotebookSyncCoordinator(
            store,
            mirror,
            () => LocalSession.SignedIn(UserA));

        var result = await sut.SyncAsync();

        result.Status.ShouldBe(NotebookSyncStatus.Completed);
        (await store.LoadAllAsync()).Single().SelectedMnemonic.ShouldBe("remote-new");
        mirror.Cards.Single().SelectedMnemonic.ShouldBe("remote-new");
    }

    [Fact]
    public async Task Sync_newer_tombstone_propagates_to_local_and_mirror()
    {
        var store = new InMemoryLocalWordCardStore();
        await store.SaveAllAsync([Card(CardConflict, UserA, "alive", T1)]);

        var mirror = new InMemoryCloudWordCardMirror();
        mirror.Seed([Card(CardConflict, UserA, "alive", T2, deletedAt: T2)]);

        var sut = new NotebookSyncCoordinator(
            store,
            mirror,
            () => LocalSession.SignedIn(UserA));

        await sut.SyncAsync();

        var local = (await store.LoadAllAsync()).Single();
        local.DeletedAtUtc.ShouldBe(T2);
        mirror.Cards.Single().DeletedAtUtc.ShouldBe(T2);
    }

    [Fact]
    public async Task Sync_when_offline_skips_without_mutating_local_or_mirror()
    {
        var store = new InMemoryLocalWordCardStore();
        await store.SaveAllAsync([Card(CardLocal, UserA, "local-only", T1)]);

        var mirror = new InMemoryCloudWordCardMirror { IsOnline = false };
        mirror.Seed([Card(CardRemote, UserA, "remote-only", T1)]);

        var sut = new NotebookSyncCoordinator(
            store,
            mirror,
            () => LocalSession.SignedIn(UserA));

        var result = await sut.SyncAsync();

        result.Status.ShouldBe(NotebookSyncStatus.SkippedOffline);
        (await store.LoadAllAsync()).Count.ShouldBe(1);
        mirror.Cards.Count.ShouldBe(1);
        mirror.Cards[0].Id.ShouldBe(CardRemote);
    }

    [Fact]
    public async Task Sync_when_anonymous_skips()
    {
        var store = new InMemoryLocalWordCardStore();
        var mirror = new InMemoryCloudWordCardMirror();
        var sut = new NotebookSyncCoordinator(store, mirror, LocalSession.Anonymous);

        var result = await sut.SyncAsync();

        result.Status.ShouldBe(NotebookSyncStatus.SkippedNotAuthenticated);
    }

    [Fact]
    public async Task Sync_does_not_push_previous_account_cards_to_current_mirror()
    {
        var store = new InMemoryLocalWordCardStore();
        await store.SaveAllAsync(
        [
            Card(CardLocal, UserA, "account-a", T1),
            Card(CardRemote, UserB, "account-b", T1)
        ]);

        var mirror = new InMemoryCloudWordCardMirror();
        var sut = new NotebookSyncCoordinator(
            store,
            mirror,
            () => LocalSession.SignedIn(UserB));

        var result = await sut.SyncAsync();

        result.Status.ShouldBe(NotebookSyncStatus.Completed);
        mirror.Cards.Count.ShouldBe(1);
        mirror.Cards[0].Id.ShouldBe(CardRemote);
        mirror.Cards[0].OwnerUserId.ShouldBe(UserB);
        mirror.Cards.ShouldNotContain(c => c.OwnerUserId == UserA);

        var local = await store.LoadAllAsync();
        local.ShouldContain(c => c.Id == CardLocal && c.OwnerUserId == UserA);
        local.ShouldContain(c => c.Id == CardRemote && c.OwnerUserId == UserB);
    }

    private static LocalWordCard Card(
        Guid id,
        Guid owner,
        string mnemonic,
        DateTimeOffset updatedAt,
        DateTimeOffset? deletedAt = null) =>
        new(
            id,
            owner,
            SourceText: "word",
            NormalizedText: "word",
            DetectedLanguage: "en",
            MeaningZh: "詞",
            Pronunciation: "wɜːd",
            SelectedMnemonic: mnemonic,
            CreatedAtUtc: T0,
            UpdatedAtUtc: updatedAt,
            DeletedAtUtc: deletedAt);
}
