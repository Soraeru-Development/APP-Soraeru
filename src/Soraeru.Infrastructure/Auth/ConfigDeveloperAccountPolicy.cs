using Microsoft.Extensions.Options;
using Soraeru.Application.Abstractions.Auth;

namespace Soraeru.Infrastructure.Auth;

public sealed class DeveloperAccountsOptions
{
    public const string SectionName = "DeveloperAccounts";

    public List<string> Emails { get; set; } = [];
}

public sealed class ConfigDeveloperAccountPolicy : IDeveloperAccountPolicy
{
    private readonly HashSet<string> _emails;

    public ConfigDeveloperAccountPolicy(IOptions<DeveloperAccountsOptions> options)
    {
        _emails = options.Value.Emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    public bool IsDeveloperEmail(string email) =>
        !string.IsNullOrWhiteSpace(email)
        && _emails.Contains(email.Trim().ToLowerInvariant());
}
