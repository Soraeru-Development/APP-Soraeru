using Microsoft.EntityFrameworkCore;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Infrastructure.Persistence.Entities;

namespace Soraeru.Infrastructure.Persistence;

public sealed class EfWordCardRepository : IWordCardRepository
{
    private readonly SoraeruDbContext _db;

    public EfWordCardRepository(SoraeruDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WordCardRecord>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.WordCards.AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        // Sqlite cannot ORDER BY DateTimeOffset in SQL; sort on the client.
        return rows
            .OrderByDescending(c => c.CreatedAt)
            .Select(ToRecord)
            .ToList();
    }

    public async Task<WordCardRecord?> GetAsync(
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.WordCards.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Id == cardId, cancellationToken);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<WordCardRecord?> FindByUserLanguageAndNormalizedAsync(
        Guid userId,
        string detectedLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default)
    {
        var language = detectedLanguage.Trim();
        var normalized = normalizedText.Trim();

        var entity = await _db.WordCards.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.UserId == userId
                    && c.DetectedLanguage.ToLower() == language.ToLower()
                    && c.NormalizedText == normalized,
                cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<WordCardRecord> AddAsync(
        WordCardRecord card,
        CancellationToken cancellationToken = default)
    {
        _db.WordCards.Add(ToEntity(card));
        await _db.SaveChangesAsync(cancellationToken);
        return card;
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.WordCards
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Id == cardId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        _db.WordCards.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.WordCards.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        _db.WordCards.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static WordCardRecord ToRecord(WordCardEntity entity) =>
        new(
            entity.Id,
            entity.UserId,
            entity.SourceText,
            entity.NormalizedText,
            entity.DetectedLanguage,
            entity.MeaningZh,
            entity.Pronunciation,
            entity.SelectedMnemonic,
            entity.CreatedAt);

    private static WordCardEntity ToEntity(WordCardRecord card) =>
        new()
        {
            Id = card.Id,
            UserId = card.UserId,
            SourceText = card.SourceText,
            NormalizedText = card.NormalizedText,
            DetectedLanguage = card.DetectedLanguage,
            MeaningZh = card.MeaningZh,
            Pronunciation = card.Pronunciation,
            SelectedMnemonic = card.SelectedMnemonic,
            CreatedAt = card.CreatedAtUtc
        };
}
