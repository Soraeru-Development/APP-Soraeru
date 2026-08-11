using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Soraeru.Application.Abstractions.Auth;

namespace Soraeru.Infrastructure.Auth;

public sealed class GoogleJsonWebSignatureIdTokenValidator : IGoogleIdTokenValidator
{
    private readonly GoogleAuthOptions _options;

    public GoogleJsonWebSignatureIdTokenValidator(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GoogleIdTokenValidationResult> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        var audiences = (_options.ClientIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (audiences.Count == 0)
        {
            return GoogleIdTokenValidationResult.Failure(
                "GOOGLE_AUTH_NOT_CONFIGURED",
                "GoogleAuth:ClientIds is not configured. Set Web (and optionally Android) OAuth client IDs via appsettings or User Secrets.");
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = audiences
            };

            // Google.Apis.Auth ValidateAsync does not accept CancellationToken in all versions.
            cancellationToken.ThrowIfCancellationRequested();
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            if (string.IsNullOrWhiteSpace(payload.Subject))
            {
                return GoogleIdTokenValidationResult.Failure(
                    "GOOGLE_TOKEN_INVALID",
                    "Google id token is missing subject.");
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return GoogleIdTokenValidationResult.Failure(
                    "GOOGLE_EMAIL_REQUIRED",
                    "Google 帳號未提供 Email，無法登入。請改用有 Email 的 Google 帳號。");
            }

            return GoogleIdTokenValidationResult.Success(
                new GoogleIdTokenPayload(payload.Subject, payload.Email, payload.Name));
        }
        catch (InvalidJwtException ex)
        {
            return GoogleIdTokenValidationResult.Failure(
                "GOOGLE_TOKEN_INVALID",
                $"Google id token validation failed: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GoogleIdTokenValidationResult.Failure(
                "GOOGLE_TOKEN_INVALID",
                $"Google id token validation failed: {ex.Message}");
        }
    }
}
