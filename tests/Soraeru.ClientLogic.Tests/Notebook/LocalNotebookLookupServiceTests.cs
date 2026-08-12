using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class LocalNotebookLookupServiceTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task FindActiveByLookupKey_when_language_usable_and_key_matches_returns_card()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        var saved = await sut.SaveAsync(new SaveLocalWordCardCommand(
            "ありがとう",
            "ありがとう",
            "ja",
            "謝謝",
            "arigatou",
            "啊哩嘎多"));
        saved.IsSuccess.ShouldBeTrue();

        var hit = await sut.FindActiveByLookupKeyAsync("  ありがとう  ", "ja");

        hit.ShouldNotBeNull();
        hit!.Id.ShouldBe(saved.Value!.Id);
    }

    [Fact]
    public async Task FindActiveByLookupKey_when_language_auto_does_not_collide()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        (await sut.SaveAsync(SampleJa())).IsSuccess.ShouldBeTrue();

        var hit = await sut.FindActiveByLookupKeyAsync("ありがとう", "auto");

        hit.ShouldBeNull();
    }

    [Fact]
    public async Task FindActiveByLookupKey_different_languages_are_distinct_cards()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        var ja = await sut.SaveAsync(new SaveLocalWordCardCommand(
            "same", "same", "ja", "日", "ja", "空耳日"));
        var en = await sut.SaveAsync(new SaveLocalWordCardCommand(
            "same", "same", "en", "英", "en", "空耳英"));
        ja.IsSuccess.ShouldBeTrue();
        en.IsSuccess.ShouldBeTrue();

        var hitJa = await sut.FindActiveByLookupKeyAsync("same", "ja");
        var hitEn = await sut.FindActiveByLookupKeyAsync("same", "en");

        hitJa.ShouldNotBeNull();
        hitEn.ShouldNotBeNull();
        hitJa!.Id.ShouldBe(ja.Value!.Id);
        hitEn!.Id.ShouldBe(en.Value!.Id);
        hitJa.Id.ShouldNotBe(hitEn.Id);
    }

    [Fact]
    public async Task FindActiveByLookupKey_after_soft_delete_returns_null()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        var saved = await sut.SaveAsync(SampleJa());
        (await sut.DeleteAsync(saved.Value!.Id)).IsSuccess.ShouldBeTrue();

        var hit = await sut.FindActiveByLookupKeyAsync("ありがとう", "ja");

        hit.ShouldBeNull();
    }

    [Fact]
    public async Task FindActiveByLookupKey_anonymous_can_hit_existing_local_card()
    {
        var store = new InMemoryLocalWordCardStore();
        var now = DateTimeOffset.Parse("2026-08-12T04:00:00Z");
        var cardId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        await store.SaveAllAsync(
        [
            new LocalWordCard(
                cardId,
                UserA,
                "ありがとう",
                "ありがとう",
                "ja",
                "謝謝",
                "arigatou",
                "啊哩嘎多",
                now,
                now,
                null)
        ]);

        var sut = new LocalNotebookService(store, LocalSession.Anonymous);
        var hit = await sut.FindActiveByLookupKeyAsync("ありがとう", "ja");

        hit.ShouldNotBeNull();
        hit!.Id.ShouldBe(cardId);
    }

    [Fact]
    public async Task FindActiveByLookupKey_authenticated_does_not_hit_other_owner_card()
    {
        var store = new InMemoryLocalWordCardStore();
        var now = DateTimeOffset.Parse("2026-08-12T05:00:00Z");
        await store.SaveAllAsync(
        [
            new LocalWordCard(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                UserB,
                "ありがとう",
                "ありがとう",
                "ja",
                "謝謝",
                "arigatou",
                "啊哩嘎多",
                now,
                now,
                null)
        ]);

        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        var hit = await sut.FindActiveByLookupKeyAsync("ありがとう", "ja");

        hit.ShouldBeNull();
    }

    private static SaveLocalWordCardCommand SampleJa() =>
        new("ありがとう", "ありがとう", "ja", "謝謝", "arigatou", "啊哩嘎多");
}
