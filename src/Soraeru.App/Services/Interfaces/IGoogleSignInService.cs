namespace Soraeru.Services.Interfaces;

/// <summary>
/// Platform Google Sign-In that returns a server-auth ID token (audience = Web Client ID).
/// </summary>
public interface IGoogleSignInService
{
    bool IsSupported { get; }

    Task<GoogleNativeSignInResult> SignInAsync(CancellationToken cancellationToken = default);
}

public sealed record GoogleNativeSignInResult(string? IdToken, string? ErrorMessage)
{
    public bool IsSuccess => !string.IsNullOrWhiteSpace(IdToken);

    public static GoogleNativeSignInResult Success(string idToken) => new(idToken, null);

    public static GoogleNativeSignInResult Fail(string message) => new(null, message);
}
