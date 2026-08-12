using Microsoft.EntityFrameworkCore;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Infrastructure.Persistence.Entities;

namespace Soraeru.Infrastructure.Persistence;

public sealed class EfWordRegenerationRepository : IWordRegenerationRepository
{
    private readonly SoraeruDbContext _db;

    public EfWordRegenerationRepository(SoraeruDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetCountAsync(
        Guid userId,
        string sourceLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default)
    {
        var language = sourceLanguage.ToLowerInvariant();
        var row = await _db.WordRegenerations.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                    && x.SourceLanguage == language
                    && x.NormalizedText == normalizedText,
                cancellationToken);
        return row?.RegenerationCount ?? 0;
    }

    public async Task IncrementAsync(
        Guid userId,
        string sourceLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default)
    {
        var language = sourceLanguage.ToLowerInvariant();
        var row = await _db.WordRegenerations
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                    && x.SourceLanguage == language
                    && x.NormalizedText == normalizedText,
                cancellationToken);

        if (row is null)
        {
            _db.WordRegenerations.Add(new WordRegenerationEntity
            {
                UserId = userId,
                SourceLanguage = language,
                NormalizedText = normalizedText,
                RegenerationCount = 1
            });
        }
        else
        {
            row.RegenerationCount += 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
