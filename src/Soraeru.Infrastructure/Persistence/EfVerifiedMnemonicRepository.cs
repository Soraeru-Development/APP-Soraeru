using Microsoft.EntityFrameworkCore;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Infrastructure.Persistence.Entities;

namespace Soraeru.Infrastructure.Persistence;

public sealed class EfVerifiedMnemonicRepository : IVerifiedMnemonicRepository
{
    private readonly SoraeruDbContext _db;

    public EfVerifiedMnemonicRepository(SoraeruDbContext db)
    {
        _db = db;
    }

    public async Task<VerifiedMnemonicRecord?> FindActiveByLanguageAndNormalizedAsync(
        string language,
        string normalizedSource,
        CancellationToken cancellationToken = default)
    {
        var lang = language.Trim();
        var normalized = normalizedSource.Trim();

        var entity = await _db.VerifiedMnemonics.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.IsEnabled
                    && e.Language.ToLower() == lang.ToLower()
                    && e.NormalizedSource == normalized,
                cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<VerifiedMnemonicRecord?> FindByLanguageAndNormalizedAsync(
        string language,
        string normalizedSource,
        CancellationToken cancellationToken = default)
    {
        var lang = language.Trim();
        var normalized = normalizedSource.Trim();

        var entity = await _db.VerifiedMnemonics.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Language.ToLower() == lang.ToLower()
                    && e.NormalizedSource == normalized,
                cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<VerifiedMnemonicRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.VerifiedMnemonics.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<VerifiedMnemonicRecord>> SearchAsync(
        string? language,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.VerifiedMnemonics.AsNoTracking().ToListAsync(cancellationToken);

        IEnumerable<VerifiedMnemonicEntity> filtered = rows;
        if (!string.IsNullOrWhiteSpace(language))
        {
            var lang = language.Trim();
            filtered = filtered.Where(e =>
                string.Equals(e.Language, lang, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            filtered = filtered.Where(e =>
                e.SourceText.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.NormalizedSource.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.DisplayText.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Explanation.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        return filtered
            .OrderByDescending(e => e.UpdatedAt)
            .Select(ToRecord)
            .ToList();
    }

    public async Task<VerifiedMnemonicRecord> AddAsync(
        VerifiedMnemonicRecord entry,
        CancellationToken cancellationToken = default)
    {
        _db.VerifiedMnemonics.Add(ToEntity(entry));
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<VerifiedMnemonicRecord> UpdateAsync(
        VerifiedMnemonicRecord entry,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.VerifiedMnemonics
            .FirstOrDefaultAsync(e => e.Id == entry.Id, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Verified mnemonic {entry.Id} not found.");
        }

        entity.Language = entry.Language;
        entity.SourceText = entry.SourceText;
        entity.NormalizedSource = entry.NormalizedSource;
        entity.DisplayText = entry.DisplayText;
        entity.NotationText = entry.NotationText;
        entity.Explanation = entry.Explanation;
        entity.IsEnabled = entry.IsEnabled;
        entity.CreatedAt = entry.CreatedAtUtc;
        entity.UpdatedAt = entry.UpdatedAtUtc;

        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    private static VerifiedMnemonicRecord ToRecord(VerifiedMnemonicEntity entity) =>
        new(
            entity.Id,
            entity.Language,
            entity.SourceText,
            entity.NormalizedSource,
            entity.DisplayText,
            entity.NotationText,
            entity.Explanation,
            entity.IsEnabled,
            entity.CreatedAt,
            entity.UpdatedAt);

    private static VerifiedMnemonicEntity ToEntity(VerifiedMnemonicRecord entry) =>
        new()
        {
            Id = entry.Id,
            Language = entry.Language,
            SourceText = entry.SourceText,
            NormalizedSource = entry.NormalizedSource,
            DisplayText = entry.DisplayText,
            NotationText = entry.NotationText,
            Explanation = entry.Explanation,
            IsEnabled = entry.IsEnabled,
            CreatedAt = entry.CreatedAtUtc,
            UpdatedAt = entry.UpdatedAtUtc
        };
}
