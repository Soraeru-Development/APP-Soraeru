using Soraeru.Application.Common;

namespace Soraeru.Application.Auth;

public interface IMeService
{
    Task<ServiceResult<MeProfile>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<MeProfile>> PatchMeAsync(
        Guid userId,
        PatchMeCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the caller's cloud notebook mirror and account (ADR-0007 Q5=A / Q9=A).
    /// </summary>
    Task<ServiceResult<bool>> DeleteAccountAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record PatchMeCommand(bool? OnboardingCompleted);

public sealed record MeProfile(
    Guid UserId,
    string Email,
    string DisplayName,
    string PlanTier,
    int DailyQuota,
    int RemainingDailyQuota,
    bool IsDeveloper,
    string NotationPref,
    bool OnboardingCompleted,
    bool HasPassword,
    bool HasGoogleSubject);
