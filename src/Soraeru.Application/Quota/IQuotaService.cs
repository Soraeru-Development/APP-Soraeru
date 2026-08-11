using Soraeru.Application.Abstractions.Persistence;

namespace Soraeru.Application.Quota;

public interface IQuotaService
{
    Task<QuotaSnapshot> GetRemainingAsync(UserRecord user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes one daily analyze unit. Developers never decrement.
    /// </summary>
    Task<bool> TryConsumeAsync(UserRecord user, CancellationToken cancellationToken = default);
}

public sealed record QuotaSnapshot(
    int DailyQuota,
    int RemainingDailyQuota,
    bool IsUnlimited);
