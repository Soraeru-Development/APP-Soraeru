using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

/// <summary>
/// Preferences-backed session store. Swap secrets to SecureStorage in W1.
/// </summary>
public sealed class PreferencesAuthSessionStore : IAuthSessionStore
{
    const string TokenKey = "auth.access_token";
    const string UserIdKey = "auth.user_id";
    const string EmailKey = "auth.email";
    const string OnboardingKey = "auth.onboarding_completed";

    public Task<string?> GetAccessTokenAsync() =>
        Task.FromResult(Preferences.Default.Get<string?>(TokenKey, null));

    public Task<Guid?> GetUserIdAsync()
    {
        var raw = Preferences.Default.Get<string?>(UserIdKey, null);
        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParseExact(raw, "N", out var id))
            return Task.FromResult<Guid?>(null);

        return Task.FromResult<Guid?>(id);
    }

    public Task<bool> GetOnboardingCompletedAsync() =>
        Task.FromResult(Preferences.Default.Get(OnboardingKey, false));

    public Task SetSessionAsync(string accessToken, Guid userId, string email, bool onboardingCompleted)
    {
        Preferences.Default.Set(TokenKey, accessToken);
        Preferences.Default.Set(UserIdKey, userId.ToString("N"));
        Preferences.Default.Set(EmailKey, email);
        Preferences.Default.Set(OnboardingKey, onboardingCompleted);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Default.Remove(TokenKey);
        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(EmailKey);
        Preferences.Default.Remove(OnboardingKey);
        return Task.CompletedTask;
    }

    public async Task<bool> HasSessionAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }
}
