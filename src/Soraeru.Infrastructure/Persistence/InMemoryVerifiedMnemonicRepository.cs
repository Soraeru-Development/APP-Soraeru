using Soraeru.Application.Abstractions.Persistence;

namespace Soraeru.Infrastructure.Persistence;

/// <summary>
/// In-memory verified mnemonics when Persistence:Provider=InMemory.
/// </summary>
public sealed class InMemoryVerifiedMnemonicRepository : IVerifiedMnemonicRepository
{
    private readonly List<VerifiedMnemonicRecord> _entries = [];
    private readonly object _gate = new();

    public Task<VerifiedMnemonicRecord?> FindActiveByLanguageAndNormalizedAsync(
        string language,
        string normalizedSource,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(
                _entries.FirstOrDefault(e =>
                    e.IsEnabled
                    && string.Equals(e.Language, language, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(e.NormalizedSource, normalizedSource, StringComparison.Ordinal)));
        }
    }

    public Task<VerifiedMnemonicRecord?> FindByLanguageAndNormalizedAsync(
        string language,
        string normalizedSource,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(
                _entries.FirstOrDefault(e =>
                    string.Equals(e.Language, language, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(e.NormalizedSource, normalizedSource, StringComparison.Ordinal)));
        }
    }

    public Task<VerifiedMnemonicRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));
        }
    }

    public Task<IReadOnlyList<VerifiedMnemonicRecord>> SearchAsync(
        string? language,
        string? query,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            IEnumerable<VerifiedMnemonicRecord> filtered = _entries;
            if (!string.IsNullOrWhiteSpace(language))
            {
                filtered = filtered.Where(e =>
                    string.Equals(e.Language, language.Trim(), StringComparison.OrdinalIgnoreCase));
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

            IReadOnlyList<VerifiedMnemonicRecord> list = filtered
                .OrderByDescending(e => e.UpdatedAtUtc)
                .ToList();
            return Task.FromResult(list);
        }
    }

    public Task<VerifiedMnemonicRecord> AddAsync(
        VerifiedMnemonicRecord entry,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _entries.Add(entry);
            return Task.FromResult(entry);
        }
    }

    public Task<VerifiedMnemonicRecord> UpdateAsync(
        VerifiedMnemonicRecord entry,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _entries.FindIndex(e => e.Id == entry.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Verified mnemonic {entry.Id} not found.");
            }

            _entries[index] = entry;
            return Task.FromResult(entry);
        }
    }
}
