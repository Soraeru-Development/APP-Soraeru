using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class LocalNotebookServiceTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task SaveAsync_when_authenticated_persists_card_visible_in_list()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));

        var result = await sut.SaveAsync(new SaveLocalWordCardCommand(
            SourceText: "hello",
            NormalizedText: "hello",
            DetectedLanguage: "en",
            MeaningZh: "你好",
            Pronunciation: "heh-loh",
            SelectedMnemonic: "哈囉"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.SourceText.ShouldBe("hello");
        result.Value.SelectedMnemonic.ShouldBe("哈囉");
        result.Value.OwnerUserId.ShouldBe(UserA);
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.DeletedAtUtc.ShouldBeNull();

        var list = await sut.ListAsync();
        list.Count.ShouldBe(1);
        list[0].Id.ShouldBe(result.Value.Id);
        list[0].MeaningZh.ShouldBe("你好");
    }

    [Fact]
    public async Task SaveAsync_when_anonymous_rejects_without_writing()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, LocalSession.Anonymous);

        var result = await sut.SaveAsync(new SaveLocalWordCardCommand(
            SourceText: "hello",
            NormalizedText: "hello",
            DetectedLanguage: "en",
            MeaningZh: "你好",
            Pronunciation: "heh-loh",
            SelectedMnemonic: "哈囉"));

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("UNAUTHORIZED");
        (await store.LoadAllAsync()).ShouldBeEmpty();
        (await sut.ListAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_when_authenticated_soft_deletes_and_hides_from_list()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        var saved = await sut.SaveAsync(SampleCommand());
        saved.IsSuccess.ShouldBeTrue();

        var deleted = await sut.DeleteAsync(saved.Value!.Id);

        deleted.IsSuccess.ShouldBeTrue();
        (await sut.ListAsync()).ShouldBeEmpty();

        var raw = await store.LoadAllAsync();
        raw.Count.ShouldBe(1);
        raw[0].DeletedAtUtc.ShouldNotBeNull();
        raw[0].UpdatedAtUtc.ShouldBe(raw[0].DeletedAtUtc!.Value);
    }

    [Fact]
    public async Task DeleteAsync_when_anonymous_rejects()
    {
        var store = new InMemoryLocalWordCardStore();
        var now = DateTimeOffset.Parse("2026-08-11T00:00:00Z");
        await store.SaveAllAsync(
        [
            new LocalWordCard(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                UserA,
                "hello",
                "hello",
                "en",
                "你好",
                "heh-loh",
                "哈囉",
                now,
                now,
                null)
        ]);

        var sut = new LocalNotebookService(store, LocalSession.Anonymous);
        var deleted = await sut.DeleteAsync(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        deleted.IsSuccess.ShouldBeFalse();
        deleted.ErrorCode.ShouldBe("UNAUTHORIZED");
        (await store.LoadAllAsync())[0].DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_when_anonymous_is_read_only_over_existing_local_cards()
    {
        var store = new InMemoryLocalWordCardStore();
        var now = DateTimeOffset.Parse("2026-08-11T01:00:00Z");
        var cardId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        await store.SaveAllAsync(
        [
            new LocalWordCard(
                cardId,
                UserA,
                "world",
                "world",
                "en",
                "世界",
                "wurld",
                "窩耳朵",
                now,
                now,
                null)
        ]);

        var sut = new LocalNotebookService(store, LocalSession.Anonymous);
        var list = await sut.ListAsync();

        list.Count.ShouldBe(1);
        list[0].Id.ShouldBe(cardId);
        list[0].SelectedMnemonic.ShouldBe("窩耳朵");

        var write = await sut.SaveAsync(SampleCommand("new", "新卡"));
        write.IsSuccess.ShouldBeFalse();
        write.ErrorCode.ShouldBe("UNAUTHORIZED");
        (await store.LoadAllAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveAsync_same_normalized_key_updates_selected_mnemonic_and_UpdatedAt()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));

        var first = await sut.SaveAsync(SampleCommand(source: "hello", mnemonic: "哈囉"));
        first.IsSuccess.ShouldBeTrue();
        var originalId = first.Value!.Id;
        var originalUpdatedAt = first.Value.UpdatedAtUtc;

        await Task.Delay(5);

        var second = await sut.SaveAsync(SampleCommand(source: "hello", mnemonic: "黑咯"));

        second.IsSuccess.ShouldBeTrue();
        second.Value.ShouldNotBeNull();
        second.Value!.Id.ShouldBe(originalId);
        second.Value.SelectedMnemonic.ShouldBe("黑咯");
        second.Value.UpdatedAtUtc.ShouldBeGreaterThan(originalUpdatedAt);

        var list = await sut.ListAsync();
        list.Count.ShouldBe(1);
        list[0].SelectedMnemonic.ShouldBe("黑咯");
        list[0].UpdatedAtUtc.ShouldBe(second.Value.UpdatedAtUtc);

        var raw = await store.LoadAllAsync();
        raw.Count.ShouldBe(1);
        raw[0].SelectedMnemonic.ShouldBe("黑咯");
    }

    [Fact]
    public async Task ClearLocalNotebookAsync_removes_all_cards()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        (await sut.SaveAsync(SampleCommand())).IsSuccess.ShouldBeTrue();
        (await sut.ListAsync()).Count.ShouldBe(1);

        await sut.ClearLocalNotebookAsync();

        (await sut.ListAsync()).ShouldBeEmpty();
        (await store.LoadAllAsync()).ShouldBeEmpty();
    }

    private static SaveLocalWordCardCommand SampleCommand(
        string source = "hello",
        string mnemonic = "哈囉") =>
        new(
            SourceText: source,
            NormalizedText: source,
            DetectedLanguage: "en",
            MeaningZh: "你好",
            Pronunciation: "heh-loh",
            SelectedMnemonic: mnemonic);
}
