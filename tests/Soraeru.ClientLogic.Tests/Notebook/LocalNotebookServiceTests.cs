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
    public async Task SaveAsync_same_normalized_key_returns_existing_without_overwriting_personal_mnemonic()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));

        var first = await sut.SaveAsync(SampleCommand(source: "hello", mnemonic: "我的黑咯"));
        first.IsSuccess.ShouldBeTrue();
        var originalId = first.Value!.Id;
        var originalUpdatedAt = first.Value.UpdatedAtUtc;

        await Task.Delay(5);

        // 再存同鍵（例如結果頁選了金標候選）不得強蓋已存個人空耳（票 17／ADR-0007）。
        var second = await sut.SaveAsync(SampleCommand(source: "hello", mnemonic: "哈囉核定"));

        second.IsSuccess.ShouldBeTrue();
        second.Value.ShouldNotBeNull();
        second.Value!.Id.ShouldBe(originalId);
        second.Value.SelectedMnemonic.ShouldBe("我的黑咯");
        second.Value.UpdatedAtUtc.ShouldBe(originalUpdatedAt);

        var list = await sut.ListAsync();
        list.Count.ShouldBe(1);
        list[0].SelectedMnemonic.ShouldBe("我的黑咯");

        var raw = await store.LoadAllAsync();
        raw.Count.ShouldBe(1);
        raw[0].SelectedMnemonic.ShouldBe("我的黑咯");
        raw[0].UpdatedAtUtc.ShouldBe(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateSelectedMnemonicAsync_when_authenticated_updates_mnemonic_and_UpdatedAt()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        var saved = await sut.SaveAsync(SampleCommand(source: "hello", mnemonic: "哈囉"));
        saved.IsSuccess.ShouldBeTrue();
        var cardId = saved.Value!.Id;
        var originalUpdatedAt = saved.Value.UpdatedAtUtc;
        var originalSource = saved.Value.SourceText;

        await Task.Delay(5);

        var updated = await sut.UpdateSelectedMnemonicAsync(cardId, "黑咯");

        updated.IsSuccess.ShouldBeTrue();
        updated.Value.ShouldNotBeNull();
        updated.Value!.Id.ShouldBe(cardId);
        updated.Value.SelectedMnemonic.ShouldBe("黑咯");
        updated.Value.SourceText.ShouldBe(originalSource);
        updated.Value.UpdatedAtUtc.ShouldBeGreaterThan(originalUpdatedAt);

        var list = await sut.ListAsync();
        list.Count.ShouldBe(1);
        list[0].SelectedMnemonic.ShouldBe("黑咯");

        var again = await sut.GetAsync(cardId);
        again.ShouldNotBeNull();
        again!.SelectedMnemonic.ShouldBe("黑咯");
    }

    [Fact]
    public async Task UpdateSelectedMnemonicAsync_when_anonymous_rejects_without_writing()
    {
        var store = new InMemoryLocalWordCardStore();
        var now = DateTimeOffset.Parse("2026-08-12T02:00:00Z");
        var cardId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
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
                now,
                now,
                null)
        ]);

        var sut = new LocalNotebookService(store, LocalSession.Anonymous);
        var result = await sut.UpdateSelectedMnemonicAsync(cardId, "黑咯");

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("UNAUTHORIZED");
        (await store.LoadAllAsync())[0].SelectedMnemonic.ShouldBe("哈囉");
        (await store.LoadAllAsync())[0].UpdatedAtUtc.ShouldBe(now);
    }

    [Fact]
    public async Task UpdateSelectedMnemonicAsync_when_blank_rejects_without_writing()
    {
        var store = new InMemoryLocalWordCardStore();
        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        var saved = await sut.SaveAsync(SampleCommand(mnemonic: "哈囉"));
        var cardId = saved.Value!.Id;
        var originalUpdatedAt = saved.Value.UpdatedAtUtc;

        var result = await sut.UpdateSelectedMnemonicAsync(cardId, "   ");

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("VALIDATION");
        (await sut.GetAsync(cardId))!.SelectedMnemonic.ShouldBe("哈囉");
        (await sut.GetAsync(cardId))!.UpdatedAtUtc.ShouldBe(originalUpdatedAt);
    }

    [Fact]
    public async Task ClearLocalNotebookAsync_removes_only_current_user_cards()
    {
        var store = new InMemoryLocalWordCardStore();
        var userB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var now = DateTimeOffset.Parse("2026-08-01T12:00:00Z");
        var cardBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
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
                now,
                now,
                null),
            new LocalWordCard(cardBId, userB, "b", "b", "en", "乙", "b", "空耳B", now, now, null)
        ]);

        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(UserA));
        await sut.ClearLocalNotebookAsync();

        (await sut.ListAsync()).ShouldBeEmpty();
        var raw = await store.LoadAllAsync();
        raw.Count.ShouldBe(1);
        raw[0].Id.ShouldBe(cardBId);
        raw[0].OwnerUserId.ShouldBe(userB);
    }

    [Fact]
    public async Task EnsureOwnerIsolationAsync_keeps_other_users_cards_in_store()
    {
        var store = new InMemoryLocalWordCardStore();
        var userB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var now = DateTimeOffset.Parse("2026-08-01T12:00:00Z");
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
                now,
                now,
                null),
            new LocalWordCard(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                userB,
                "b",
                "b",
                "en",
                "乙",
                "b",
                "空耳B",
                now,
                now,
                null)
        ]);

        var sut = new LocalNotebookService(store, () => LocalSession.SignedIn(userB));
        await sut.EnsureOwnerIsolationAsync(userB);

        var raw = await store.LoadAllAsync();
        raw.Count.ShouldBe(2);
        raw.ShouldContain(c => c.OwnerUserId == UserA);
        raw.ShouldContain(c => c.OwnerUserId == userB);
        (await sut.ListAsync()).Count.ShouldBe(1);
        (await sut.ListAsync())[0].OwnerUserId.ShouldBe(userB);
    }

    [Fact]
    public async Task Logout_relogin_and_switch_users_retain_each_owners_cards()
    {
        var store = new InMemoryLocalWordCardStore();
        var userB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        LocalSession session = LocalSession.SignedIn(UserA);
        var sut = new LocalNotebookService(store, () => session);

        (await sut.SaveAsync(SampleCommand(source: "alpha", mnemonic: "空耳A"))).IsSuccess.ShouldBeTrue();

        // Explicit logout keeps SoT; session becomes anonymous for UI but store rows remain.
        session = LocalSession.Anonymous();
        (await store.LoadAllAsync()).Count.ShouldBe(1);

        session = LocalSession.SignedIn(UserA);
        (await sut.ListAsync()).Count.ShouldBe(1);
        (await sut.ListAsync())[0].SelectedMnemonic.ShouldBe("空耳A");

        session = LocalSession.SignedIn(userB);
        await SignInNotebookIsolation.ApplyAsync(sut, UserA, userB);
        (await sut.ListAsync()).ShouldBeEmpty();
        (await sut.SaveAsync(SampleCommand(source: "beta", mnemonic: "空耳B"))).IsSuccess.ShouldBeTrue();

        session = LocalSession.SignedIn(UserA);
        await SignInNotebookIsolation.ApplyAsync(sut, userB, UserA);
        var aList = await sut.ListAsync();
        aList.Count.ShouldBe(1);
        aList[0].SelectedMnemonic.ShouldBe("空耳A");
        (await store.LoadAllAsync()).Count.ShouldBe(2);
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
