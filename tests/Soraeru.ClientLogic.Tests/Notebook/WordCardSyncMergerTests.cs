using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class WordCardSyncMergerTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CardOnlyLocal = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CardOnlyRemote = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CardBoth = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-01T10:00:00Z");
    private static readonly DateTimeOffset T1 = DateTimeOffset.Parse("2026-08-01T11:00:00Z");
    private static readonly DateTimeOffset T2 = DateTimeOffset.Parse("2026-08-01T12:00:00Z");

    [Fact]
    public void Merge_union_includes_cards_present_on_only_one_side()
    {
        var local = Card(CardOnlyLocal, "local-only", T1);
        var remote = Card(CardOnlyRemote, "remote-only", T1);

        var merged = WordCardSyncMerger.Merge([local], [remote]);

        merged.Count.ShouldBe(2);
        merged.ShouldContain(c => c.Id == CardOnlyLocal && c.SelectedMnemonic == "local-only");
        merged.ShouldContain(c => c.Id == CardOnlyRemote && c.SelectedMnemonic == "remote-only");
    }

    [Fact]
    public void Merge_same_id_newer_UpdatedAt_whole_card_wins()
    {
        var local = Card(CardBoth, "local-old", T1);
        var remote = Card(CardBoth, "remote-new", T2);

        var merged = WordCardSyncMerger.Merge([local], [remote]);

        merged.Count.ShouldBe(1);
        merged[0].SelectedMnemonic.ShouldBe("remote-new");
        merged[0].UpdatedAtUtc.ShouldBe(T2);
    }

    [Fact]
    public void Merge_same_id_local_newer_keeps_local_whole_card()
    {
        var local = Card(CardBoth, "local-new", T2);
        var remote = Card(CardBoth, "remote-old", T1);

        var merged = WordCardSyncMerger.Merge([local], [remote]);

        merged.Count.ShouldBe(1);
        merged[0].SelectedMnemonic.ShouldBe("local-new");
    }

    [Fact]
    public void Merge_newer_tombstone_wins_over_older_live_card()
    {
        var local = Card(CardBoth, "still-alive", T1);
        var remote = Card(CardBoth, "still-alive", T2, deletedAt: T2);

        var merged = WordCardSyncMerger.Merge([local], [remote]);

        merged.Count.ShouldBe(1);
        merged[0].DeletedAtUtc.ShouldBe(T2);
        merged[0].UpdatedAtUtc.ShouldBe(T2);
    }

    [Fact]
    public void Merge_newer_live_card_wins_over_older_tombstone()
    {
        var local = Card(CardBoth, "revived", T2);
        var remote = Card(CardBoth, "old", T1, deletedAt: T1);

        var merged = WordCardSyncMerger.Merge([local], [remote]);

        merged.Count.ShouldBe(1);
        merged[0].DeletedAtUtc.ShouldBeNull();
        merged[0].SelectedMnemonic.ShouldBe("revived");
    }

    [Fact]
    public void Merge_equal_UpdatedAt_keeps_local_side()
    {
        var local = Card(CardBoth, "local-tie", T1);
        var remote = Card(CardBoth, "remote-tie", T1);

        var merged = WordCardSyncMerger.Merge([local], [remote]);

        merged.Count.ShouldBe(1);
        merged[0].SelectedMnemonic.ShouldBe("local-tie");
    }

    private static LocalWordCard Card(
        Guid id,
        string mnemonic,
        DateTimeOffset updatedAt,
        DateTimeOffset? deletedAt = null) =>
        new(
            id,
            UserA,
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
