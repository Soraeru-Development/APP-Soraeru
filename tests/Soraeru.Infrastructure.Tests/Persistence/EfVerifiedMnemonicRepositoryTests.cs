using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Infrastructure.Persistence;

namespace Soraeru.Infrastructure.Tests.Persistence;

public sealed class EfVerifiedMnemonicRepositoryTests
{
    [Fact]
    public async Task AddAsync_survives_new_DbContext_and_finds_active_by_language_normalized()
    {
        var path = Path.Combine(Path.GetTempPath(), $"soraeru-verified-{Guid.NewGuid():N}.db");
        try
        {
            var entryId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var createdAt = DateTimeOffset.Parse("2026-08-10T12:00:00Z");
            var entry = new VerifiedMnemonicRecord(
                entryId,
                "en",
                "hello",
                "hello",
                "哈囉核定",
                "ㄏㄚ ㄌㄨㄛˊ",
                "策展提示",
                IsEnabled: true,
                createdAt,
                createdAt);

            await using (var writeDb = CreateDb(path))
            {
                await writeDb.Database.EnsureCreatedAsync();
                var writeRepo = new EfVerifiedMnemonicRepository(writeDb);
                await writeRepo.AddAsync(entry);
            }

            await using (var readDb = CreateDb(path))
            {
                var readRepo = new EfVerifiedMnemonicRepository(readDb);
                var active = await readRepo.FindActiveByLanguageAndNormalizedAsync("en", "hello");
                active.ShouldNotBeNull();
                active!.Id.ShouldBe(entryId);
                active.DisplayText.ShouldBe("哈囉核定");

                var disabled = entry with { IsEnabled = false, UpdatedAtUtc = createdAt.AddHours(1) };
                await readRepo.UpdateAsync(disabled);
            }

            await using (var afterDisable = CreateDb(path))
            {
                var repo = new EfVerifiedMnemonicRepository(afterDisable);
                (await repo.FindActiveByLanguageAndNormalizedAsync("en", "hello")).ShouldBeNull();
                var any = await repo.FindByLanguageAndNormalizedAsync("en", "hello");
                any.ShouldNotBeNull();
                any!.IsEnabled.ShouldBeFalse();
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
        }
    }
}
