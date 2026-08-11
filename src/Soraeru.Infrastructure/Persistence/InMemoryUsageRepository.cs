using Soraeru.Application.Abstractions.Persistence;

namespace Soraeru.Infrastructure.Persistence;

/// <summary>
/// In-memory daily usage counter until EF maps UsageDaily.
/// </summary>
public sealed class InMemoryUsageRepository : IUsageRepository
{
    private readonly Dictionary<(Guid UserId, DateOnly Date), int> _counts = new();
    private readonly object _gate = new();

    public Task<int> GetTodayCountAsync(
        Guid userId,
        DateOnly utcDate,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_counts.GetValueOrDefault((userId, utcDate)));
        }
    }

    public Task IncrementAsync(
        Guid userId,
        DateOnly utcDate,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var key = (userId, utcDate);
            _counts[key] = _counts.GetValueOrDefault(key) + 1;
            return Task.CompletedTask;
        }
    }
}
