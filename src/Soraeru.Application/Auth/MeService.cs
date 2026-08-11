using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Application.Common;
using Soraeru.Application.Quota;

namespace Soraeru.Application.Auth;

public sealed class MeService : IMeService
{
    private readonly IUserRepository _users;
    private readonly IQuotaService _quota;
    private readonly IWordCardRepository _cards;

    public MeService(IUserRepository users, IQuotaService quota, IWordCardRepository cards)
    {
        _users = users;
        _quota = quota;
        _cards = cards;
    }

    public async Task<ServiceResult<MeProfile>> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return ServiceResult<MeProfile>.Failure("VALIDATION", "User id is required.");
        }

        var user = await _users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<MeProfile>.Failure("NOT_FOUND", "User not found.");
        }

        return ServiceResult<MeProfile>.Success(await ToProfileAsync(user, cancellationToken));
    }

    public async Task<ServiceResult<MeProfile>> PatchMeAsync(
        Guid userId,
        PatchMeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return ServiceResult<MeProfile>.Failure("VALIDATION", "User id is required.");
        }

        if (command.OnboardingCompleted is null)
        {
            return ServiceResult<MeProfile>.Failure("VALIDATION", "No fields to update.");
        }

        var user = await _users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<MeProfile>.Failure("NOT_FOUND", "User not found.");
        }

        if (user.OnboardingCompleted != command.OnboardingCompleted.Value)
        {
            user = user with { OnboardingCompleted = command.OnboardingCompleted.Value };
            await _users.UpdateAsync(user, cancellationToken);
        }

        return ServiceResult<MeProfile>.Success(await ToProfileAsync(user, cancellationToken));
    }

    public async Task<ServiceResult<bool>> DeleteAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return ServiceResult<bool>.Failure("VALIDATION", "User id is required.");
        }

        var user = await _users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<bool>.Failure("NOT_FOUND", "User not found.");
        }

        // Cloud notebook mirror first, then account row (ADR-0007).
        await _cards.DeleteAllByUserAsync(userId, cancellationToken);
        await _users.DeleteAsync(userId, cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private async Task<MeProfile> ToProfileAsync(UserRecord user, CancellationToken cancellationToken)
    {
        var quota = await _quota.GetRemainingAsync(user, cancellationToken);
        return new MeProfile(
            user.Id,
            user.Email,
            user.DisplayName,
            user.PlanTier,
            quota.DailyQuota,
            quota.RemainingDailyQuota,
            user.IsDeveloper,
            user.NotationPref,
            user.OnboardingCompleted,
            HasPassword: !string.IsNullOrEmpty(user.PasswordHash),
            HasGoogleSubject: !string.IsNullOrEmpty(user.GoogleSubject));
    }
}
