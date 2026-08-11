namespace Soraeru.Application.Abstractions.Auth;

/// <summary>
/// Outbound mail (password reset). SMTP details live in Infrastructure.
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}
