using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class JsonFileLocalWordCardStoreTests
{
    [Fact]
    public async Task Save_then_new_store_instance_loads_same_cards()
    {
        var path = Path.Combine(Path.GetTempPath(), $"soraeru-wordcards-{Guid.NewGuid():N}.json");
        try
        {
            var userId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var now = DateTimeOffset.Parse("2026-08-11T02:00:00Z");
            var card = new LocalWordCard(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                userId,
                "persist",
                "persist",
                "en",
                "持久",
                "per-sist",
                "波希斯特",
                now,
                now,
                null);

            var writer = new JsonFileLocalWordCardStore(path);
            await writer.SaveAllAsync([card]);

            var reader = new JsonFileLocalWordCardStore(path);
            var loaded = await reader.LoadAllAsync();

            loaded.Count.ShouldBe(1);
            loaded[0].Id.ShouldBe(card.Id);
            loaded[0].SourceText.ShouldBe("persist");
            loaded[0].SelectedMnemonic.ShouldBe("波希斯特");
            loaded[0].OwnerUserId.ShouldBe(userId);
            loaded[0].UpdatedAtUtc.ShouldBe(now);
            loaded[0].DeletedAtUtc.ShouldBeNull();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAllAsync_missing_file_returns_empty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"soraeru-wordcards-missing-{Guid.NewGuid():N}.json");
        var store = new JsonFileLocalWordCardStore(path);

        var loaded = await store.LoadAllAsync();

        loaded.ShouldBeEmpty();
    }
}
