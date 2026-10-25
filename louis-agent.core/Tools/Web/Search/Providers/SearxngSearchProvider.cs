namespace LouisAgent.Core.Tools.Web.Search.Providers;

using LouisAgent.Core.Tools.Web.Fetching;
using System.Net;
using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Messages;

/// <summary>
/// SearXNG as the search engine (F32 step 1): the metasearch we run ourselves in a compose stack, for development,
/// tests and benchmarks; the sold endpoint keeps a licensed API, since the engines SearXNG asks don't license their
/// results (ADR 0023). <c>GET {SEARXNG_URL}/search?q=…&amp;format=json</c>: <see cref="WebSearchQuery.Site"/> goes into
/// the query as <c>site:</c>, <see cref="WebSearchQuery.Recency"/> becomes <c>time_range</c> (day, week, month, year:
/// SearXNG's own words). SearXNG names the engines it asked and couldn't reach (<c>unresponsive_engines</c>): when none
/// answered, the search fails with each one's reason; when some did, the others are logged. JSON is off in SearXNG
/// unless its settings list it (a 403 otherwise). See https://docs.searxng.org/dev/search_api.html.
/// </summary>
public sealed class SearxngSearchProvider(Uri baseUrl, HttpClient? http = null) : IWebSearchProvider
{
    private static readonly ILogger Log = AgentLogging.For<SearxngSearchProvider>();

    private readonly HttpClient _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    public string Name => "SearXNG";

    public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default)
    {
        var uri = RequestUri(baseUrl, query);
        using var response = await _http.GetAsync(uri, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new WebSearchException(MessageText.Format(WebToolMessage.SearxngJsonOff, new { Url = baseUrl }));
        if (!response.IsSuccessStatusCode)
            throw new WebSearchException(MessageText.Format(WebToolMessage.SearxngFailed, new { Status = (int)response.StatusCode, Detail = response.ReasonPhrase ?? "" }));

        var answer = Parse(json, query.MaxResults);
        if (answer.SilentEngines.Count > 0)
            Log.Log(LogLevel.Warning, LogMessage.SearxngEnginesSilent, new { Engines = string.Join("; ", answer.SilentEngines) });
        return answer.Results;
    }

    /// <summary>The search URL under SearXNG's own address, which may carry a path (<c>/searxng/</c>).</summary>
    internal static Uri RequestUri(Uri baseUrl, WebSearchQuery query)
    {
        string text = string.IsNullOrWhiteSpace(query.Site) ? query.Text : $"{query.Text} site:{query.Site.Trim()}";
        var parameters = new List<string> { "q=" + Uri.EscapeDataString(text), "format=json" };
        if (query.Recency is { } recency) parameters.Add("time_range=" + recency.ToString().ToLowerInvariant());
        string root = baseUrl.ToString().TrimEnd('/');
        return new Uri($"{root}/search?{string.Join("&", parameters)}");
    }

    /// <summary>The results, cleaned and cut to <paramref name="maxResults"/>, and the engines that didn't answer.</summary>
    internal static SearxngAnswer Parse(string json, int maxResults)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var results = root.TryGetProperty("results", out var items) && items.ValueKind == JsonValueKind.Array
            ? items.EnumerateArray()
                .Select(r => new WebSearchResult(
                    HtmlText.CleanInline(r.TryGetProperty("title", out var t) ? t.GetString() : ""),
                    r.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                    HtmlText.CleanInline(r.TryGetProperty("content", out var c) ? c.GetString() : "")))
                .Where(r => r.Url.Length > 0)
                .Take(maxResults)
                .ToList()
            : [];

        // Each silent engine is [name, reason].
        var silent = root.TryGetProperty("unresponsive_engines", out var engines) && engines.ValueKind == JsonValueKind.Array
            ? engines.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Array && e.GetArrayLength() > 0)
                .Select(e => e.GetArrayLength() > 1 ? $"{e[0].GetString()}: {e[1].GetString()}" : e[0].GetString() ?? "?")
                .ToList()
            : [];

        if (results.Count == 0 && silent.Count > 0)
            throw new WebSearchException(MessageText.Format(WebToolMessage.SearxngNoEngineAnswered, new { Reasons = string.Join("; ", silent) }));
        return new SearxngAnswer(results, silent);
    }
}
