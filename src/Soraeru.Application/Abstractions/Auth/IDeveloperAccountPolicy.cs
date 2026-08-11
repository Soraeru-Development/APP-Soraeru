namespace Soraeru.Application.Abstractions.Auth;

/// <summary>
/// Central allowlist for developer accounts (unlimited quotas / skipped limits).
/// </summary>
public interface IDeveloperAccountPolicy
{
    bool IsDeveloperEmail(string email);
}
