using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Infrastructure.Persistence;

namespace Soraeru.Infrastructure.Tests.Persistence;

/// <summary>
/// Proves notebook cards survive closing the database connection (durable file store),
/// mirroring Users/Usage Sqlite behaviour — not process-local memory.
/// </summary>
public sealed class EfWordCardRepositoryTests
{
    [Fact]
    public async Task AddAsync_survives_new_DbContext_on_same_sqlite_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"soraeru-wordcards-{Guid.NewGuid():N}.db");
        try
        {
            var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var cardId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var createdAt = DateTimeOffset.Parse("2026-08-10T09:00:00Z");

            var card = new WordCardRecord(
                cardId,
                userId,
                "สวัสดี",
                "สวัสดี",
                "th",
                "你好",
                "sa-wat-dee",
                "薩瓦地",
                createdAt);

            await using (var writeDb = CreateDb(path))
            {
                await writeDb.Database.EnsureCreatedAsync();
                var writeRepo = new EfWordCardRepository(writeDb);
                await writeRepo.AddAsync(card);
            }

            await using (var readDb = CreateDb(path))
            {
                var readRepo = new EfWordCardRepository(readDb);
                var listed = await readRepo.ListByUserAsync(userId);

                listed.Count.ShouldBe(1);
                var loaded = listed[0];
                loaded.Id.ShouldBe(cardId);
                loaded.UserId.ShouldBe(userId);
                loaded.SourceText.ShouldBe("สวัสดี");
                loaded.NormalizedText.ShouldBe("สวัสดี");
                loaded.DetectedLanguage.ShouldBe("th");
                loaded.MeaningZh.ShouldBe("你好");
                loaded.Pronunciation.ShouldBe("sa-wat-dee");
                loaded.SelectedMnemonic.ShouldBe("薩瓦地");
                loaded.CreatedAtUtc.ShouldBe(createdAt);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDelete(path);
        }
    }

    [Fact]
    public async Task DeleteAsync_removes_only_owner_card_durably()
    {
        var path = Path.Combine(Path.GetTempPath(), $"soraeru-wordcards-{Guid.NewGuid():N}.db");
        try
        {
            var ownerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var strangerId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
            var cardId = Guid.Parse("11111111-2222-3333-4444-555555555555");

            await using (var writeDb = CreateDb(path))
            {
                await writeDb.Database.EnsureCreatedAsync();
                var writeRepo = new EfWordCardRepository(writeDb);
                await writeRepo.AddAsync(new WordCardRecord(
                    cardId,
                    ownerId,
                    "hello",
                    "hello",
                    "en",
                    "你好",
                    "həˈləʊ",
                    "哈囉",
                    DateTimeOffset.Parse("2026-08-10T09:00:00Z")));
            }

            await using (var mutateDb = CreateDb(path))
            {
                var repo = new EfWordCardRepository(mutateDb);
                await repo.DeleteAsync(strangerId, cardId);
                (await repo.ListByUserAsync(ownerId)).Count.ShouldBe(1);

                await repo.DeleteAsync(ownerId, cardId);
            }

            await using (var readDb = CreateDb(path))
            {
                var repo = new EfWordCardRepository(readDb);
                (await repo.ListByUserAsync(ownerId)).Count.ShouldBe(0);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDelete(path);
        }
    }

    private static SoraeruDbContext CreateDb(string path)
    {
        var options = new DbContextOptionsBuilder<SoraeruDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False")
            .Options;
        return new SoraeruDbContext(options);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup; Windows may hold the handle briefly.
        }
    }
}
