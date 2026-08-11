namespace Soraeru.Application.Abstractions.Auth;

public interface IPasswordResetTokenStore
{
    Task StoreAsync(string token, Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task<Guid?> TakeUserIdAsync(string token, CancellationToken cancellationToken = default);
}
