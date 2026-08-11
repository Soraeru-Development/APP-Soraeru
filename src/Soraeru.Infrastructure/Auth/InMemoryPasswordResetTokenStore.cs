using Soraeru.Application.Abstractions.Auth;

namespace Soraeru.Infrastructure.Auth;

public sealed class InMemoryPasswordResetTokenStore : IPasswordResetTokenStore
{
    private readonly Dictionary<string, (Guid UserId, DateTimeOffset ExpiresAt)> _tokens = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public Task StoreAsync(
        string token,
        Guid userId,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _tokens[token] = (userId, DateTimeOffset.UtcNow.Add(lifetime));
        }

        return Task.CompletedTask;
    }

    public Task<Guid?> TakeUserIdAsync(string token, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_tokens.Remove(token, out var entry))
            {
                return Task.FromResult<Guid?>(null);
            }

            if (entry.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return Task.FromResult<Guid?>(null);
            }

            return Task.FromResult<Guid?>(entry.UserId);
        }
    }
}
