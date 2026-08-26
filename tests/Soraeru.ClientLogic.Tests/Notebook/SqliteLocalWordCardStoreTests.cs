using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class SqliteLocalWordCardStoreTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-01T10:00:00Z");

    [Fact]
    public async Task Save_then_new_instance_loads_same_cards_for_multiple_owners()
    {
        var dbPath = TempDbPath();
        try
        {
            var cardA = Card(Guid.Parse("11111111-1111-1111-1111-111111111111"), UserA, "a", "空耳A");
            var cardB = Card(Guid.Parse("22222222-2222-2222-2222-222222222222"), UserB, "b", "空耳B");

            var writer = new SqliteLocalWordCardStore(dbPath);
            await writer.SaveAllAsync([cardA, cardB]);

            var reader = new SqliteLocalWordCardStore(dbPath);
            var loaded = await reader.LoadAllAsync();

            loaded.Count.ShouldBe(2);
            loaded.ShouldContain(c => c.Id == cardA.Id && c.OwnerUserId == UserA && c.SelectedMnemonic == "空耳A");
            loaded.ShouldContain(c => c.Id == cardB.Id && c.OwnerUserId == UserB && c.SelectedMnemonic == "空耳B");
        }
        finally
        {
            TryDelete(dbPath);
        }
    }

    [Fact]
    public async Task SaveAllAsync_replaces_entire_table_including_keeping_multi_owner_rows()
    {
        var dbPath = TempDbPath();
        try
        {
            var store = new SqliteLocalWordCardStore(dbPath);
            var cardA = Card(Guid.Parse("11111111-1111-1111-1111-111111111111"), UserA, "a", "空耳A");
            var cardB = Card(Guid.Parse("22222222-2222-2222-2222-222222222222"), UserB, "b", "空耳B");
            await store.SaveAllAsync([cardA, cardB]);

            var cardA2 = cardA with { SelectedMnemonic = "空耳A2", UpdatedAtUtc = T0.AddHours(1) };
            await store.SaveAllAsync([cardA2, cardB]);

            var loaded = await store.LoadAllAsync();
            loaded.Count.ShouldBe(2);
            loaded.Single(c => c.OwnerUserId == UserA).SelectedMnemonic.ShouldBe("空耳A2");
            loaded.Single(c => c.OwnerUserId == UserB).SelectedMnemonic.ShouldBe("空耳B");
        }
        finally
        {
            TryDelete(dbPath);
        }
    }

    [Fact]
    public async Task Constructor_migrates_legacy_json_once_into_sqlite()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"soraeru-migrate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "local-wordcards.db");
        var jsonPath = Path.Combine(dir, "local-wordcards.json");
        try
        {
            var cardA = Card(Guid.Parse("11111111-1111-1111-1111-111111111111"), UserA, "legacy", "舊卡");
            var jsonStore = new JsonFileLocalWordCardStore(jsonPath);
            await jsonStore.SaveAllAsync([cardA]);

            var sqlite = new SqliteLocalWordCardStore(dbPath, legacyJsonPath: jsonPath);
            var loaded = await sqlite.LoadAllAsync();

            loaded.Count.ShouldBe(1);
            loaded[0].Id.ShouldBe(cardA.Id);
            loaded[0].SelectedMnemonic.ShouldBe("舊卡");
            File.Exists(jsonPath).ShouldBeFalse();
            File.Exists(jsonPath + ".migrated").ShouldBeTrue();
        }
        finally
        {
            TryDelete(dbPath);
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private static LocalWordCard Card(Guid id, Guid owner, string text, string mnemonic) =>
        new(id, owner, text, text, "en", "義", "pro", mnemonic, T0, T0, null);

    private static string TempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"soraeru-wordcards-{Guid.NewGuid():N}.db");

    private static void TryDelete(string dbPath)
    {
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var p = dbPath + suffix;
            try
            {
                if (File.Exists(p))
                    File.Delete(p);
            }
            catch (IOException)
            {
                // Best-effort cleanup; temp files may linger briefly on Windows.
            }
        }
    }
}
