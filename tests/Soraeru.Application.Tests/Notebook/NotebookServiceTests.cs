using NSubstitute;
using Shouldly;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Application.Notebook;

namespace Soraeru.Application.Tests.Notebook;

public sealed class NotebookServiceTests
{
    private readonly IWordCardRepository _cards = Substitute.For<IWordCardRepository>();
    private readonly NotebookService _sut;

    public NotebookServiceTests()
    {
        _sut = new NotebookService(_cards);
    }

    [Fact]
    public async Task SaveAsync_persists_card_bound_to_user_with_selected_mnemonic()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        WordCardRecord? added = null;
        _cards.FindByUserLanguageAndNormalizedAsync(
                userId,
                "th",
                "สวัสดี",
                Arg.Any<CancellationToken>())
            .Returns((WordCardRecord?)null);
        _cards.AddAsync(Arg.Any<WordCardRecord>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                added = ci.Arg<WordCardRecord>();
                return added;
            });

        var result = await _sut.SaveAsync(
            new SaveNotebookCardCommand(
                userId,
                "  สวัสดี  ",
                "th",
                "你好",
                "sa-wat-dee",
                "薩瓦地"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.SourceText.ShouldBe("สวัสดี");
        result.Value.DetectedLanguage.ShouldBe("th");
        result.Value.MeaningZh.ShouldBe("你好");
        result.Value.Pronunciation.ShouldBe("sa-wat-dee");
        result.Value.SelectedMnemonic.ShouldBe("薩瓦地");
        result.Value.Id.ShouldNotBe(Guid.Empty);

        added.ShouldNotBeNull();
        added!.UserId.ShouldBe(userId);
        added.NormalizedText.ShouldBe("สวัสดี");
        added.SelectedMnemonic.ShouldBe("薩瓦地");
        added.UpdatedAtUtc.ShouldBe(added.CreatedAtUtc);
        added.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_returns_only_caller_cards_newest_first()
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var older = new WordCardRecord(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            userId,
            "hello",
            "hello",
            "en",
            "你好",
            "he-loh",
            "哈囉",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var newer = new WordCardRecord(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            userId,
            "ありがとう",
            "ありがとう",
            "ja",
            "謝謝",
            "a-ri-ga-tou",
            "阿里嘎多",
            DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-06-01T00:00:00Z"));

        _cards.ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { newer, older });

        var result = await _sut.ListAsync(userId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(2);
        result.Value[0].Id.ShouldBe(newer.Id);
        result.Value[0].SelectedMnemonic.ShouldBe("阿里嘎多");
        result.Value[1].Id.ShouldBe(older.Id);
    }

    [Fact]
    public async Task GetAsync_returns_card_for_owner_and_not_found_for_other_user()
    {
        var ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var strangerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var cardId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var record = new WordCardRecord(
            cardId,
            ownerId,
            "salamat",
            "salamat",
            "fil",
            "謝謝",
            "sa-la-mat",
            "沙拉馬",
            DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-03-01T00:00:00Z"));

        _cards.GetAsync(ownerId, cardId, Arg.Any<CancellationToken>()).Returns(record);
        _cards.GetAsync(strangerId, cardId, Arg.Any<CancellationToken>()).Returns((WordCardRecord?)null);

        var owned = await _sut.GetAsync(ownerId, cardId);
        owned.IsSuccess.ShouldBeTrue();
        owned.Value!.SourceText.ShouldBe("salamat");
        owned.Value.SelectedMnemonic.ShouldBe("沙拉馬");

        var denied = await _sut.GetAsync(strangerId, cardId);
        denied.IsSuccess.ShouldBeFalse();
        denied.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task DeleteAsync_writes_tombstone_for_owned_card_and_rejects_missing()
    {
        var userId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var cardId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var missingId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var created = DateTimeOffset.Parse("2026-02-01T00:00:00Z");

        _cards.GetAsync(userId, cardId, Arg.Any<CancellationToken>())
            .Returns(new WordCardRecord(
                cardId,
                userId,
                "bonjour",
                "bonjour",
                "fr",
                "你好",
                "bon-joor",
                "蹦洙",
                created,
                created));
        _cards.GetAsync(userId, missingId, Arg.Any<CancellationToken>())
            .Returns((WordCardRecord?)null);

        var deleted = await _sut.DeleteAsync(userId, cardId);
        deleted.IsSuccess.ShouldBeTrue();
        deleted.Value.ShouldBeTrue();
        await _cards.Received(1).UpsertAsync(
            Arg.Is<WordCardRecord>(r =>
                r.Id == cardId
                && r.UserId == userId
                && r.DeletedAtUtc != null
                && r.UpdatedAtUtc == r.DeletedAtUtc),
            Arg.Any<CancellationToken>());
        await _cards.DidNotReceive().DeleteAsync(userId, cardId, Arg.Any<CancellationToken>());

        var missing = await _sut.DeleteAsync(userId, missingId);
        missing.IsSuccess.ShouldBeFalse();
        missing.ErrorCode.ShouldBe("NOT_FOUND");
        await _cards.DidNotReceive().UpsertAsync(
            Arg.Is<WordCardRecord>(r => r.Id == missingId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_duplicate_same_user_language_normalized_returns_existing_without_add()
    {
        var userId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var existingId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var existing = new WordCardRecord(
            existingId,
            userId,
            "Hello",
            "Hello",
            "en",
            "你好",
            "he-loh",
            "哈囉",
            DateTimeOffset.Parse("2026-04-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-04-01T00:00:00Z"));

        _cards.FindByUserLanguageAndNormalizedAsync(
                userId,
                "en",
                "Hello",
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.SaveAsync(
            new SaveNotebookCardCommand(
                userId,
                " Hello ",
                "en",
                "嗨",
                "HEL-oh",
                "吼樓"));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(existingId);
        result.Value.SelectedMnemonic.ShouldBe("哈囉");
        await _cards.DidNotReceive().AddAsync(Arg.Any<WordCardRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PullMirrorAsync_returns_all_rows_including_tombstones_with_stable_ids_and_timestamps()
    {
        var userId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var aliveId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
        var tombstoneId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");
        var created = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var updated = DateTimeOffset.Parse("2026-02-01T00:00:00Z");
        var deletedAt = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

        _cards.ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new WordCardRecord(
                    aliveId,
                    userId,
                    "hello",
                    "hello",
                    "en",
                    "你好",
                    "he-loh",
                    "哈囉",
                    created,
                    updated),
                new WordCardRecord(
                    tombstoneId,
                    userId,
                    "bye",
                    "bye",
                    "en",
                    "再見",
                    "bai",
                    "拜",
                    created,
                    deletedAt,
                    deletedAt)
            });

        var result = await _sut.PullMirrorAsync(userId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(2);

        var alive = result.Value.Single(c => c.Id == aliveId);
        alive.OwnerUserId.ShouldBe(userId);
        alive.NormalizedText.ShouldBe("hello");
        alive.UpdatedAtUtc.ShouldBe(updated);
        alive.DeletedAtUtc.ShouldBeNull();

        var tombstone = result.Value.Single(c => c.Id == tombstoneId);
        tombstone.DeletedAtUtc.ShouldBe(deletedAt);
        tombstone.UpdatedAtUtc.ShouldBe(deletedAt);
    }

    [Fact]
    public async Task ListAsync_hides_tombstones()
    {
        var userId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var aliveId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333");
        var tombstoneId = Guid.Parse("bbbbbbbb-4444-4444-4444-444444444444");
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var t1 = DateTimeOffset.Parse("2026-02-01T00:00:00Z");

        _cards.ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new WordCardRecord(aliveId, userId, "a", "a", "en", "甲", "a", "啊", t0, t0),
                new WordCardRecord(tombstoneId, userId, "b", "b", "en", "乙", "b", "哔", t0, t1, t1)
            });

        var result = await _sut.ListAsync(userId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Id.ShouldBe(aliveId);
    }

    [Fact]
    public async Task PushMirrorAsync_applies_whole_card_LWW_upsert_without_replace_all()
    {
        var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var keepId = Guid.Parse("aaaaaaaa-5555-5555-5555-555555555555");
        var conflictId = Guid.Parse("bbbbbbbb-6666-6666-6666-666666666666");
        var newId = Guid.Parse("cccccccc-7777-7777-7777-777777777777");
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var t1 = DateTimeOffset.Parse("2026-02-01T00:00:00Z");
        var t2 = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

        var existingKeep = new WordCardRecord(
            keepId, userId, "keep", "keep", "en", "留", "keep", "舊留", t0, t1);
        var existingConflict = new WordCardRecord(
            conflictId, userId, "old", "old", "en", "舊", "old", "舊空耳", t0, t1);

        _cards.GetAsync(userId, keepId, Arg.Any<CancellationToken>()).Returns(existingKeep);
        _cards.GetAsync(userId, conflictId, Arg.Any<CancellationToken>()).Returns(existingConflict);
        _cards.GetAsync(userId, newId, Arg.Any<CancellationToken>()).Returns((WordCardRecord?)null);

        var incoming = new[]
        {
            new MirrorWordCard(
                conflictId, userId, "new", "new", "en", "新", "new", "新空耳", t0, t2, null),
            new MirrorWordCard(
                newId, userId, "fresh", "fresh", "en", "新卡", "fresh", "新卡空耳", t2, t2, null)
            // keepId intentionally omitted — must not be deleted (no replace-all)
        };

        var result = await _sut.PushMirrorAsync(userId, incoming);

        result.IsSuccess.ShouldBeTrue();
        await _cards.Received(1).UpsertAsync(
            Arg.Is<WordCardRecord>(r =>
                r.Id == conflictId
                && r.SelectedMnemonic == "新空耳"
                && r.UpdatedAtUtc == t2),
            Arg.Any<CancellationToken>());
        await _cards.Received(1).UpsertAsync(
            Arg.Is<WordCardRecord>(r => r.Id == newId && r.SelectedMnemonic == "新卡空耳"),
            Arg.Any<CancellationToken>());
        await _cards.DidNotReceive().UpsertAsync(
            Arg.Is<WordCardRecord>(r => r.Id == keepId),
            Arg.Any<CancellationToken>());
        await _cards.DidNotReceive().DeleteAsync(userId, keepId, Arg.Any<CancellationToken>());
        await _cards.DidNotReceive().DeleteAllByUserAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushMirrorAsync_newer_tombstone_wins_over_older_alive_row()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var cardId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var t1 = DateTimeOffset.Parse("2026-02-01T00:00:00Z");
        var t2 = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

        _cards.GetAsync(userId, cardId, Arg.Any<CancellationToken>())
            .Returns(new WordCardRecord(cardId, userId, "x", "x", "en", "字", "x", "活", t0, t1));

        var result = await _sut.PushMirrorAsync(
            userId,
            [
                new MirrorWordCard(cardId, userId, "x", "x", "en", "字", "x", "活", t0, t2, t2)
            ]);

        result.IsSuccess.ShouldBeTrue();
        await _cards.Received(1).UpsertAsync(
            Arg.Is<WordCardRecord>(r =>
                r.Id == cardId
                && r.DeletedAtUtc == t2
                && r.UpdatedAtUtc == t2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushMirrorAsync_keeps_existing_when_incoming_updated_at_is_not_newer()
    {
        var userId = Guid.Parse("eeeeeeee-ffff-0000-1111-222222222222");
        var cardId = Guid.Parse("ffffffff-0000-1111-2222-333333333333");
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var t1 = DateTimeOffset.Parse("2026-02-01T00:00:00Z");

        _cards.GetAsync(userId, cardId, Arg.Any<CancellationToken>())
            .Returns(new WordCardRecord(cardId, userId, "x", "x", "en", "字", "x", "雲端新", t0, t1));

        var result = await _sut.PushMirrorAsync(
            userId,
            [
                new MirrorWordCard(cardId, userId, "x", "x", "en", "字", "x", "本機舊", t0, t0, null)
            ]);

        result.IsSuccess.ShouldBeTrue();
        await _cards.DidNotReceive().UpsertAsync(Arg.Any<WordCardRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushMirrorAsync_rejects_cards_owned_by_another_user()
    {
        var userId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        var otherId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000");
        var cardId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111");
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        var result = await _sut.PushMirrorAsync(
            userId,
            [
                new MirrorWordCard(cardId, otherId, "x", "x", "en", "字", "x", "偷", t0, t0, null)
            ]);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("FORBIDDEN");
        await _cards.DidNotReceive().UpsertAsync(Arg.Any<WordCardRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushMirrorAsync_rejects_when_card_id_already_owned_by_another_user()
    {
        var userId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        var otherId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000");
        var cardId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111");
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        _cards.GetAsync(userId, cardId, Arg.Any<CancellationToken>()).Returns((WordCardRecord?)null);
        _cards.GetByIdAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new WordCardRecord(cardId, otherId, "x", "x", "en", "字", "x", "他帳", t0, t0));

        var result = await _sut.PushMirrorAsync(
            userId,
            [
                new MirrorWordCard(cardId, userId, "x", "x", "en", "字", "x", "撞Id", t0, t0, null)
            ]);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("CONFLICT");
        await _cards.DidNotReceive().UpsertAsync(Arg.Any<WordCardRecord>(), Arg.Any<CancellationToken>());
    }
}
