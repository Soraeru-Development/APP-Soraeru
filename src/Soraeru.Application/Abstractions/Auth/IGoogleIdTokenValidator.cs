namespace Soraeru.Application.Abstractions.Auth;

/// <summary>
/// Validates a Google ID token and extracts identity claims. Implemented in Infrastructure.
/// </summary>
public interface IGoogleIdTokenValidator
{
    Task<GoogleIdTokenValidationResult> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default);
}

public sealed record GoogleIdTokenPayload(string Subject, string Email, string? Name);

public sealed class GoogleIdTokenValidationResult
{
    public bool IsSuccess { get; }
    public GoogleIdTokenPayload? Payload { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private GoogleIdTokenValidationResult(
        bool isSuccess,
        GoogleIdTokenPayload? payload,
        string? errorCode,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        Payload = payload;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static GoogleIdTokenValidationResult Success(GoogleIdTokenPayload payload) =>
        new(true, payload, null, null);

    public static GoogleIdTokenValidationResult Failure(string errorCode, string errorMessage) =>
        new(false, null, errorCode, errorMessage);
}
