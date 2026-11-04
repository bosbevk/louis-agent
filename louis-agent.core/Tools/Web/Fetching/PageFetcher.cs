using LouisAgent.Core.Sessions.Replay;
using System.Net;
using LouisAgent.Core.Guardrails.Network;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Replay;
using LouisAgent.Core.Tools.Web.Robots;
using LouisAgent.Core.Sessions.Replay.Store;

namespace LouisAgent.Core.Tools.Web.Fetching;

/// <summary>
/// The guarded client the web tools fetch pages with (S, CL15, from WebTools): it refuses private, loopback and
/// link-local addresses through <see cref="SsrfGuard"/>, the one rule in one place, on every redirect hop and again at
/// connect time, so the agent cannot be used to reach internal services. One per toolset, shared by the tools that
/// fetch. It is also where fetches are recorded and replayed (F18 step 4b): given a store, its handler is a
/// <see cref="RecordedFetchHandler"/> outside the guard's, and a replay skips the per-hop DNS check, because nothing
/// leaves the process then (<see cref="EnsurePublicAsync"/>, the renderer's check, stays). Given a
/// <see cref="RobotsPolicy"/> (ADR 0068) it also obeys the site's robots.txt on every hop and the page's own no-AI
/// signals on the answer.
/// </summary>
public sealed class PageFetcher
{
    private const int MaxRedirects = 5;
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) louis-agent/1.0";

    /// <summary>Responses over this are refused.</summary>
    public const long MaxResponseBytes = 5 * 1024 * 1024;

    private readonly HttpClient _http;
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _resolve;
    private readonly ReplayStore? _replay;
    private readonly RobotsPolicy? _robots;

    /// <param name="handler">Test seam; production uses a handler that re-checks the address at connect time.</param>
    /// <param name="resolve">Test seam for DNS resolution.</param>
    /// <param name="replay">The process's store of recorded web answers, or null where none is kept.</param>
    /// <param name="robots">
    /// The policy every hop and every answer is checked against, or null for none. The fetcher that reads robots.txt
    /// for the policy is built without one.
    /// </param>
    public PageFetcher(HttpMessageHandler? handler = null,
        Func<string, CancellationToken, Task<IPAddress[]>>? resolve = null,
        ReplayStore? replay = null, RobotsPolicy? robots = null)
    {
        _resolve = resolve ?? ((host, ct) => Dns.GetHostAddressesAsync(host, ct));
        _replay = replay;
        _robots = robots;
        var inner = handler ?? SsrfGuard.CreateGuardedHandler();
        _http = new HttpClient(replay is null ? inner : new RecordedFetchHandler(replay) { InnerHandler = inner },
            disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(20),
            MaxResponseContentBufferSize = MaxResponseBytes,
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    /// <summary>
    /// A replay answers from the store and sends nothing, so the hop's address needs no lookup (decision 6 of step 4b).
    /// </summary>
    private bool Replaying => _replay is not null && ReplayTrial.Current is { Replaying: true };

    /// <summary>
    /// Refuses an internal address before anything is sent (to the page, or to a third party that renders it).
    /// </summary>
    public Task EnsurePublicAsync(Uri uri) => SsrfGuard.EnsurePublicHostAsync(uri, _resolve);

    /// <summary>
    /// Follows up to <see cref="MaxRedirects"/> redirects, checking every hop's address before requesting it, and,
    /// with a robots policy, every hop's path against the site's robots.txt and the final answer against its own
    /// no-AI signals. A refusal throws as the address guard does, so the tools report it the same way.
    /// </summary>
    public async Task<(HttpResponseMessage Response, Uri FinalUri)> GetFollowingRedirectsAsync(Uri uri)
    {
        for (int hop = 0; ; hop++)
        {
            if (!Replaying) await SsrfGuard.EnsurePublicHostAsync(uri, _resolve);
            await RefuseIfRobotsDisallowAsync(uri);
            var response = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));
            if ((int)response.StatusCode is < 300 or >= 400 || response.Headers.Location is null)
            {
                await RefuseIfPageSaysNoAsync(response, uri);
                return (response, uri);
            }

            response.Dispose();
            if (hop == MaxRedirects)
                throw new HttpRequestException(MessageText.Format(WebToolMessage.FetchTooManyRedirects,
                    new { Count = MaxRedirects }));
            var next = response.Headers.Location.IsAbsoluteUri
                ? response.Headers.Location
                : new Uri(uri, response.Headers.Location);
            if (next.Scheme is not ("http" or "https"))
                throw new BlockedAddressException(MessageText.Format(WebToolMessage.FetchRedirectScheme,
                    new { next.Scheme }));
            uri = next;
        }
    }

    /// <summary>
    /// Stops before anything is sent if the site's robots.txt says this address is not ours to read, or can't be read.
    /// </summary>
    private async Task RefuseIfRobotsDisallowAsync(Uri uri)
    {
        if (_robots is null) return;

        if (await _robots.RefusalAsync(uri) is { } refusal) throw new BlockedAddressException(refusal);
    }

    /// <summary>
    /// Stops if the page that answered says no to AI readers in a header or its meta tag. A web page's body is read
    /// into memory here (it is capped), so whoever asked can still read it after.
    /// </summary>
    private async Task RefuseIfPageSaysNoAsync(HttpResponseMessage response, Uri uri)
    {
        if (_robots is null) return;

        var headers = response.Headers.Concat(response.Content.Headers)
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value), StringComparer.OrdinalIgnoreCase);
        string? html = await HtmlBodyAsync(response);
        if (_robots.PageRefusal(uri, headers, html) is not { } refusal) return;

        response.Dispose();
        throw new BlockedAddressException(refusal);
    }

    /// <summary>The answer's body when it is a web page, else null (an image or a PDF has no meta tag).</summary>
    private static async Task<string?> HtmlBodyAsync(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType?.MediaType is not ("text/html" or "application/xhtml+xml"))
            return null;

        await response.Content.LoadIntoBufferAsync(MaxResponseBytes);
        return await response.Content.ReadAsStringAsync();
    }
}
