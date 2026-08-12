using Soraeru.Application.Abstractions.Persistence;

namespace Soraeru.Infrastructure.Persistence;

/// <summary>
/// In-memory WordCards when Persistence:Provider=InMemory.
/// </summary>
public sealed class InMemoryWordCardRepository : IWordCardRepository
{
    private readonly List<WordCardRecord> _cards = [];
    private readonly object _gate = new();

    public Task<IReadOnlyList<WordCardRecord>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            IReadOnlyList<WordCardRecord> list = _cards
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAtUtc)
                .ToList();
            return Task.FromResult(list);
        }
    }

    public Task<WordCardRecord?> GetAsync(
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(
                _cards.FirstOrDefault(c => c.UserId == userId && c.Id == cardId));
        }
    }

    public Task<WordCardRecord?> GetByIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_cards.FirstOrDefault(c => c.Id == cardId));
        }
    }

    public Task<WordCardRecord?> FindByUserLanguageAndNormalizedAsync(
        Guid userId,
        string detectedLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(
                _cards.FirstOrDefault(c =>
                    c.UserId == userId
                    && c.DeletedAtUtc is null
                    && string.Equals(c.DetectedLanguage, detectedLanguage, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(c.NormalizedText, normalizedText, StringComparison.Ordinal)));
        }
    }

    public Task<WordCardRecord> AddAsync(
        WordCardRecord card,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _cards.Add(card);
            return Task.FromResult(card);
        }
    }

    public Task UpsertAsync(WordCardRecord card, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _cards.FindIndex(c => c.Id == card.Id);
            if (index < 0)
            {
                _cards.Add(card);
            }
            else if (_cards[index].UserId != card.UserId)
            {
                throw new WordCardIdConflictException(card.Id);
            }
            else
            {
                _cards[index] = card;
            }

            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid userId, Guid cardId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _cards.RemoveAll(c => c.UserId == userId && c.Id == cardId);
            return Task.CompletedTask;
        }
    }

    public Task DeleteAllByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _cards.RemoveAll(c => c.UserId == userId);
            return Task.CompletedTask;
        }
    }
}
