using Soraeru.Application.Common;

namespace Soraeru.Application.Curator;

public interface ICuratorMnemonicService
{
    Task<ServiceResult<VerifiedMnemonicDto>> CreateAsync(
        CreateVerifiedMnemonicCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<VerifiedMnemonicDto>> UpdateAsync(
        UpdateVerifiedMnemonicCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<VerifiedMnemonicDto>> SetEnabledAsync(
        SetVerifiedMnemonicEnabledCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<VerifiedMnemonicDto>>> ListAsync(
        Guid actorUserId,
        string? language,
        string? query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<VerifiedMnemonicDto>> GetAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default);
}

public sealed record CreateVerifiedMnemonicCommand(
    Guid ActorUserId,
    string Language,
    string SourceText,
    string DisplayText,
    string NotationText,
    string Explanation,
    bool IsEnabled = true);

public sealed record UpdateVerifiedMnemonicCommand(
    Guid ActorUserId,
    Guid Id,
    string DisplayText,
    string NotationText,
    string Explanation,
    bool? IsEnabled = null);

public sealed record SetVerifiedMnemonicEnabledCommand(
    Guid ActorUserId,
    Guid Id,
    bool IsEnabled);

public sealed record VerifiedMnemonicDto(
    Guid Id,
    string Language,
    string SourceText,
    string NormalizedSource,
    string DisplayText,
    string NotationText,
    string Explanation,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
