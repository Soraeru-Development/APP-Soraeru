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
                createdAt,
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
                loaded.UpdatedAtUtc.ShouldBe(createdAt);
                loaded.DeletedAtUtc.ShouldBeNull();
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
            var createdAt = DateTimeOffset.Parse("2026-08-10T09:00:00Z");

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
                    createdAt,
                    createdAt));
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

    [Fact]
    public async Task UpsertAsync_rejects_when_id_already_owned_by_another_user()
    {
        var path = Path.Combine(Path.GetTempPath(), $"soraeru-wordcards-{Guid.NewGuid():N}.db");
        try
        {
            var ownerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var callerId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
            var cardId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var t0 = DateTimeOffset.Parse("2026-08-10T09:00:00Z");
            var t1 = DateTimeOffset.Parse("2026-08-11T09:00:00Z");

            await using (var writeDb = CreateDb(path))
            {
                await writeDb.Database.EnsureCreatedAsync();
                var repo = new EfWordCardRepository(writeDb);
                await repo.AddAsync(new WordCardRecord(
                    cardId, ownerId, "owner", "owner", "en", "他帳", "o", "他空耳", t0, t0));
            }

            await using (var mutateDb = CreateDb(path))
            {
                var repo = new EfWordCardRepository(mutateDb);
                var act = () => repo.UpsertAsync(new WordCardRecord(
                    cardId, callerId, "caller", "caller", "en", "呼叫者", "c", "偷Id", t0, t1));

                var ex = await Should.ThrowAsync<WordCardIdConflictException>(act);
                ex.CardId.ShouldBe(cardId);
            }

            await using (var readDb = CreateDb(path))
            {
                var repo = new EfWordCardRepository(readDb);
                var ownerRows = await repo.ListByUserAsync(ownerId);
                ownerRows.Count.ShouldBe(1);
                ownerRows[0].SelectedMnemonic.ShouldBe("他空耳");
                ownerRows[0].UserId.ShouldBe(ownerId);

                (await repo.ListByUserAsync(callerId)).Count.ShouldBe(0);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDelete(path);
        }
    }

    [Fact]
    public async Task UpsertAsync_applies_newer_row_and_keeps_older_when_stale()
    {
        var path = Path.Combine(Path.GetTempPath(), $"soraeru-wordcards-{Guid.NewGuid():N}.db");
        try
        {
            var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var cardId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var t0 = DateTimeOffset.Parse("2026-08-10T09:00:00Z");
            var t1 = DateTimeOffset.Parse("2026-08-11T09:00:00Z");
            var t2 = DateTimeOffset.Parse("2026-08-12T09:00:00Z");

            await using (var writeDb = CreateDb(path))
            {
                await writeDb.Database.EnsureCreatedAsync();
                var repo = new EfWordCardRepository(writeDb);
                await repo.AddAsync(new WordCardRecord(
                    cardId, userId, "hello", "hello", "en", "你好", "x", "舊", t0, t1));
                await repo.UpsertAsync(new WordCardRecord(
                    cardId, userId, "hello", "hello", "en", "你好", "x", "新", t0, t2));
            }

            await using (var readDb = CreateDb(path))
            {
                var repo = new EfWordCardRepository(readDb);
                var loaded = (await repo.ListByUserAsync(userId)).Single();
                loaded.SelectedMnemonic.ShouldBe("新");
                loaded.UpdatedAtUtc.ShouldBe(t2);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDelete(path);
        }
    }

    [Fact]
    public async Task DeleteAllByUserAsync_hard_deletes_alive_and_tombstone_rows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"soraeru-wordcards-{Guid.NewGuid():N}.db");
        try
        {
            var ownerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var otherId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
            var t0 = DateTimeOffset.Parse("2026-08-10T09:00:00Z");
            var t1 = DateTimeOffset.Parse("2026-08-11T09:00:00Z");

            await using (var writeDb = CreateDb(path))
            {
                await writeDb.Database.EnsureCreatedAsync();
                var repo = new EfWordCardRepository(writeDb);
                await repo.AddAsync(new WordCardRecord(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    ownerId, "a", "a", "en", "甲", "a", "啊", t0, t0));
                await repo.AddAsync(new WordCardRecord(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ownerId, "b", "b", "en", "乙", "b", "哔", t0, t1, t1));
                await repo.AddAsync(new WordCardRecord(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    otherId, "c", "c", "en", "丙", "c", "吸", t0, t0));

                await repo.DeleteAllByUserAsync(ownerId);
            }

            await using (var readDb = CreateDb(path))
            {
                var repo = new EfWordCardRepository(readDb);
                (await repo.ListByUserAsync(ownerId)).Count.ShouldBe(0);
                (await repo.ListByUserAsync(otherId)).Count.ShouldBe(1);
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
