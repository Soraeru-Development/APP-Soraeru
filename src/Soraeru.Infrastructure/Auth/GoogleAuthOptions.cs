namespace Soraeru.Infrastructure.Auth;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    /// <summary>
    /// OAuth client IDs accepted as ID token audience (typically Web + Android client IDs).
    /// </summary>
    public List<string> ClientIds { get; set; } = [];
}
