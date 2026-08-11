namespace Soraeru.Application.Abstractions.Persistence;

/// <summary>
/// Daily analyze-quota tracking (UsageDaily).
/// </summary>
public interface IUsageRepository
{
    Task<int> GetTodayCountAsync(Guid userId, DateOnly utcDate, CancellationToken cancellationToken = default);

    Task IncrementAsync(Guid userId, DateOnly utcDate, CancellationToken cancellationToken = default);
}
