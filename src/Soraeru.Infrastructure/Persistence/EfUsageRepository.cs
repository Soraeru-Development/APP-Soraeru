using Microsoft.EntityFrameworkCore;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Infrastructure.Persistence.Entities;

namespace Soraeru.Infrastructure.Persistence;

public sealed class EfUsageRepository : IUsageRepository
{
    private readonly SoraeruDbContext _db;

    public EfUsageRepository(SoraeruDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetTodayCountAsync(
        Guid userId,
        DateOnly utcDate,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.UsageDaily.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.UsageDate == utcDate, cancellationToken);
        return row?.AnalyzeCount ?? 0;
    }

    public async Task IncrementAsync(
        Guid userId,
        DateOnly utcDate,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.UsageDaily
            .FirstOrDefaultAsync(x => x.UserId == userId && x.UsageDate == utcDate, cancellationToken);

        if (row is null)
        {
            _db.UsageDaily.Add(new UsageDailyEntity
            {
                UserId = userId,
                UsageDate = utcDate,
                AnalyzeCount = 1
            });
        }
        else
        {
            row.AnalyzeCount += 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
