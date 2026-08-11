using Microsoft.Extensions.Logging;
using Soraeru.Application.Abstractions.Auth;

namespace Soraeru.Infrastructure.Email;

/// <summary>
/// Logs reset links instead of sending SMTP mail. Swap for real SMTP in W1.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset for {Email}: {ResetLink}", email, resetLink);
        return Task.CompletedTask;
    }
}
