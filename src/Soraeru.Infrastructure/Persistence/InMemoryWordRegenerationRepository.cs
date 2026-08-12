using Soraeru.Application.Abstractions.Persistence;

namespace Soraeru.Infrastructure.Persistence;

public sealed class InMemoryWordRegenerationRepository : IWordRegenerationRepository
{
    private readonly Dictionary<(Guid UserId, string Language, string NormalizedText), int> _counts = new();
    private readonly object _gate = new();

    public Task<int> GetCountAsync(
        Guid userId,
        string sourceLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_counts.GetValueOrDefault(Key(userId, sourceLanguage, normalizedText)));
        }
    }

    public Task IncrementAsync(
        Guid userId,
        string sourceLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var key = Key(userId, sourceLanguage, normalizedText);
            _counts[key] = _counts.GetValueOrDefault(key) + 1;
            return Task.CompletedTask;
        }
    }

    private static (Guid, string, string) Key(Guid userId, string sourceLanguage, string normalizedText) =>
        (userId, sourceLanguage.ToLowerInvariant(), normalizedText);
}
