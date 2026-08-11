namespace Soraeru.Application.Abstractions.Persistence;

/// <summary>
/// Persistence boundary for curated verified empty-ear (空耳) entries — not learner WordCards.
/// </summary>
public interface IVerifiedMnemonicRepository
{
    Task<VerifiedMnemonicRecord?> FindActiveByLanguageAndNormalizedAsync(
        string language,
        string normalizedSource,
        CancellationToken cancellationToken = default);

    Task<VerifiedMnemonicRecord?> FindByLanguageAndNormalizedAsync(
        string language,
        string normalizedSource,
        CancellationToken cancellationToken = default);

    Task<VerifiedMnemonicRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VerifiedMnemonicRecord>> SearchAsync(
        string? language,
        string? query,
        CancellationToken cancellationToken = default);

    Task<VerifiedMnemonicRecord> AddAsync(
        VerifiedMnemonicRecord entry,
        CancellationToken cancellationToken = default);

    Task<VerifiedMnemonicRecord> UpdateAsync(
        VerifiedMnemonicRecord entry,
        CancellationToken cancellationToken = default);
}

public sealed record VerifiedMnemonicRecord(
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
