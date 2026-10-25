namespace LouisAgent.Core.Tools.Web.Fetching;

using System.Net;
using LouisAgent.Core.Guardrails.Network;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;

/// <summary>
/// The guarded client the web tools fetch pages with (S, CL15, from WebTools): it refuses private, loopback and
/// link-local addresses through <see cref="SsrfGuard"/>, the one rule in one place, on every redirect hop and again at
/// connect time, so the agent cannot be used to reach internal services. One per toolset, shared by the tools that
/// fetch; the seam Phase 3 step 4b records and replays fetches at.
/// </summary>
public sealed class PageFetcher
{
    private const int MaxRedirects = 5;
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) louis-agent/1.0";

    /// <summary>Responses over this are refused.</summary>
    public const long MaxResponseBytes = 5 * 1024 * 1024;

    private readonly HttpClient _http;
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _resolve;

    /// <param name="handler">Test seam; production uses a handler that re-checks the address at connect time.</param>
    /// <param name="resolve">Test seam for DNS resolution.</param>
    public PageFetcher(HttpMessageHandler? handler = null, Func<string, CancellationToken, Task<IPAddress[]>>? resolve = null)
    {
        _resolve = resolve ?? ((host, ct) => Dns.GetHostAddressesAsync(host, ct));
        _http = new HttpClient(handler ?? SsrfGuard.CreateGuardedHandler(), disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(20),
            MaxResponseContentBufferSize = MaxResponseBytes,
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    /// <summary>Refuses an internal address before anything is sent (to the page, or to a third party that renders it).</summary>
    public Task EnsurePublicAsync(Uri uri) => SsrfGuard.EnsurePublicHostAsync(uri, _resolve);

    /// <summary>Follows up to <see cref="MaxRedirects"/> redirects, checking every hop's address before requesting it.</summary>
    public async Task<(HttpResponseMessage Response, Uri FinalUri)> GetFollowingRedirectsAsync(Uri uri)
    {
        for (int hop = 0; ; hop++)
        {
            await SsrfGuard.EnsurePublicHostAsync(uri, _resolve);
            var response = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));
            if ((int)response.StatusCode is < 300 or >= 400 || response.Headers.Location is null) return (response, uri);

            response.Dispose();
            if (hop == MaxRedirects) throw new HttpRequestException(MessageText.Format(WebToolMessage.FetchTooManyRedirects, new { Count = MaxRedirects }));
            var next = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(uri, response.Headers.Location);
            if (next.Scheme is not ("http" or "https")) throw new BlockedAddressException(MessageText.Format(WebToolMessage.FetchRedirectScheme, new { next.Scheme }));
            uri = next;
        }
    }
}
