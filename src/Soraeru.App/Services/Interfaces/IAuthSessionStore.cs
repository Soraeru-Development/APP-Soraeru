namespace Soraeru.Services.Interfaces;

/// <summary>
/// Secure storage for access token and basic session flags.
/// </summary>
public interface IAuthSessionStore
{
    Task<string?> GetAccessTokenAsync();

    Task<Guid?> GetUserIdAsync();

    Task<bool> GetOnboardingCompletedAsync();

    Task SetSessionAsync(string accessToken, Guid userId, string email, bool onboardingCompleted);

    Task ClearAsync();

    Task<bool> HasSessionAsync();
}
