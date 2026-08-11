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
    public async Task DeleteAsync_removes_owned_card_and_rejects_missing()
    {
        var userId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var cardId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var missingId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

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
                DateTimeOffset.Parse("2026-02-01T00:00:00Z")));
        _cards.GetAsync(userId, missingId, Arg.Any<CancellationToken>())
            .Returns((WordCardRecord?)null);

        var deleted = await _sut.DeleteAsync(userId, cardId);
        deleted.IsSuccess.ShouldBeTrue();
        deleted.Value.ShouldBeTrue();
        await _cards.Received(1).DeleteAsync(userId, cardId, Arg.Any<CancellationToken>());

        var missing = await _sut.DeleteAsync(userId, missingId);
        missing.IsSuccess.ShouldBeFalse();
        missing.ErrorCode.ShouldBe("NOT_FOUND");
        await _cards.DidNotReceive().DeleteAsync(userId, missingId, Arg.Any<CancellationToken>());
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
}
