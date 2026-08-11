using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Application.Common;

namespace Soraeru.Application.Quota;

public sealed class QuotaService : IQuotaService
{
    private readonly IUsageRepository _usage;

    public QuotaService(IUsageRepository usage)
    {
        _usage = usage;
    }

    public async Task<QuotaSnapshot> GetRemainingAsync(
        UserRecord user,
        CancellationToken cancellationToken = default)
    {
        if (user.IsDeveloper)
        {
            return new QuotaSnapshot(
                AppConstants.UnlimitedDailyQuota,
                AppConstants.UnlimitedDailyQuota,
                IsUnlimited: true);
        }

        var used = await _usage.GetTodayCountAsync(user.Id, TodayUtc(), cancellationToken);
        var remaining = Math.Max(0, user.DailyQuota - used);
        return new QuotaSnapshot(user.DailyQuota, remaining, IsUnlimited: false);
    }

    public async Task<bool> TryConsumeAsync(
        UserRecord user,
        CancellationToken cancellationToken = default)
    {
        if (user.IsDeveloper)
        {
            return true;
        }

        var snapshot = await GetRemainingAsync(user, cancellationToken);
        if (snapshot.RemainingDailyQuota <= 0)
        {
            return false;
        }

        await _usage.IncrementAsync(user.Id, TodayUtc(), cancellationToken);
        return true;
    }

    private static DateOnly TodayUtc() => DateOnly.FromDateTime(DateTime.UtcNow);
}
