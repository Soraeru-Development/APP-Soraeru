using System.Net.Http.Headers;
using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Api;

/// <summary>
/// Attaches Bearer token from the local session store to outbound API calls.
/// </summary>
public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthSessionStore _session;

    public AuthHeaderHandler(IAuthSessionStore session)
    {
        _session = session;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _session.GetAccessTokenAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
