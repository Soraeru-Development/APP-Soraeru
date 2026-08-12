namespace Soraeru.Application.Abstractions.Persistence;

/// <summary>
/// Persistence boundary for notebook word cards (cloud mirror rows).
/// </summary>
public interface IWordCardRepository
{
    /// <summary>All rows for the user, including tombstones.</summary>
    Task<IReadOnlyList<WordCardRecord>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<WordCardRecord?> GetAsync(Guid userId, Guid cardId, CancellationToken cancellationToken = default);

    /// <summary>Global PK lookup — WordCards.Id is unique across users.</summary>
    Task<WordCardRecord?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<WordCardRecord?> FindByUserLanguageAndNormalizedAsync(
        Guid userId,
        string detectedLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default);

    Task<WordCardRecord> AddAsync(WordCardRecord card, CancellationToken cancellationToken = default);

    Task UpsertAsync(WordCardRecord card, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, Guid cardId, CancellationToken cancellationToken = default);

    /// <summary>Hard-delete all cloud-mirror cards for account deletion (ADR-0007), including tombstones.</summary>
    Task DeleteAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record WordCardRecord(
    Guid Id,
    Guid UserId,
    string SourceText,
    string NormalizedText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeletedAtUtc = null);
