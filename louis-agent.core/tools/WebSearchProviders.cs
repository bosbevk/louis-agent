namespace louis_agent.core.tools;

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using louis_agent.core.config;

/// <summary>A search request: <see cref="Recency"/> is null or one of day, week, month, year.</summary>
public sealed record WebSearchQuery(string Text, int MaxResults, string? Site = null, string? Recency = null);

public sealed record WebSearchResult(string Title, string Url, string Snippet);

/// <summary>
/// A web search engine behind <see cref="WebTools.WebSearch"/>. Implement this to plug in another engine
/// (Bing, SearXNG, an internal search, a test fake) and pass it to <see cref="AgentEngine"/>.
/// </summary>
public interface IWebSearchProvider
{
    /// <summary>Shown to the model, e.g. "Google".</summary>
    string Name { get; }

    /// <summary>
    /// Returns up to <see cref="WebSearchQuery.MaxResults"/> results. Throw <see cref="WebSearchException"/> for
    /// problems the user can act on (bad key, quota, blocked); its message is shown as is.
    /// </summary>
    Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default);
}

/// <summary>A search failure with an actionable message for the user.</summary>
public sealed class WebSearchException(string message) : Exception(message);

/// <summary>
/// Picks the search engine from configuration: WEB_SEARCH_PROVIDER = google | duckduckgo. When unset, Google is
/// used if GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID are both set, otherwise keyless DuckDuckGo.
/// Misconfiguration never fails startup; searches report what is missing instead.
/// </summary>
public static class WebSearchProviderFactory
{
    public static IReadOnlyList<string> SupportedProviders { get; } = ["google", "duckduckgo"];

    public static IWebSearchProvider Create(AgentOptions options, HttpClient? http = null)
    {
        bool hasGoogle = !string.IsNullOrWhiteSpace(options.GoogleSearchApiKey) && !string.IsNullOrWhiteSpace(options.GoogleSearchEngineId);
        string choice = (options.WebSearchProvider ?? "").Trim().ToLowerInvariant();

        return choice switch
        {
            "" => hasGoogle ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http) : new DuckDuckGoSearchProvider(http),
            "google" => hasGoogle
                ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http)
                : new UnavailableSearchProvider("Google", "WEB_SEARCH_PROVIDER=google needs both GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID in config/.env.secrets."),
            "duckduckgo" or "ddg" => new DuckDuckGoSearchProvider(http),
            _ => new UnavailableSearchProvider(choice, $"Unknown WEB_SEARCH_PROVIDER '{choice}'. Supported: {string.Join(", ", SupportedProviders)}."),
        };
    }
}

/// <summary>Stands in for a provider that is selected but not usable, so the agent can tell the user why.</summary>
public sealed class UnavailableSearchProvider(string name, string reason) : IWebSearchProvider
{
    public string Name => name;

    public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default) =>
        throw new WebSearchException(reason);
}

/// <summary>
/// Google Programmable Search Engine via the Custom Search JSON API. Needs an API key and a search engine ID (cx)
/// configured to search the entire web. See https://developers.google.com/custom-search/v1/overview.
/// </summary>
public sealed class GoogleSearchProvider(string apiKey, string searchEngineId, HttpClient? http = null) : IWebSearchProvider
{
    private readonly HttpClient _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

    public string Name => "Google";

    public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"key={Uri.EscapeDataString(apiKey)}",
            $"cx={Uri.EscapeDataString(searchEngineId)}",
            $"q={Uri.EscapeDataString(query.Text)}",
            $"num={Math.Clamp(query.MaxResults, 1, 10)}",
        };
        if (!string.IsNullOrWhiteSpace(query.Site)) parameters.AddRange([$"siteSearch={Uri.EscapeDataString(query.Site)}", "siteSearchFilter=i"]);
        if (query.Recency is { } recency) parameters.Add($"dateRestrict={recency[0]}1");

        using var response = await _http.GetAsync("https://www.googleapis.com/customsearch/v1?" + string.Join('&', parameters), cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string detail = TryReadError(json) ?? response.ReasonPhrase ?? "";
            throw new WebSearchException(response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => $"Google search quota exceeded ({detail}). Try again later or raise the quota in Google Cloud.",
                HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized =>
                    $"Google rejected the request ({detail}). Check GOOGLE_SEARCH_API_KEY, GOOGLE_SEARCH_ENGINE_ID and that the Custom Search API is enabled.",
                _ => $"Google search failed: HTTP {(int)response.StatusCode} {detail}",
            });
        }

        return ParseResults(json, query.MaxResults);
    }

    internal static List<WebSearchResult> ParseResults(string json, int count)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items)) return []; // no "items" means no results

        return items.EnumerateArray()
            .Select(i => new WebSearchResult(
                WebTools.CleanInline(i.TryGetProperty("title", out var t) ? t.GetString() : ""),
                i.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "",
                WebTools.CleanInline(i.TryGetProperty("snippet", out var s) ? s.GetString() : "")))
            .Where(r => r.Url.Length > 0)
            .Take(count)
            .ToList();
    }

    private static string? TryReadError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("error").GetProperty("message").GetString();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }
}

/// <summary>Keyless search through DuckDuckGo's HTML endpoint. Convenient, but it may refuse automated traffic.</summary>
public sealed partial class DuckDuckGoSearchProvider(HttpClient? http = null) : IWebSearchProvider
{
    private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) louis-agent/1.0";
    private readonly HttpClient _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

    public string Name => "DuckDuckGo";

    public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default)
    {
        string text = string.IsNullOrWhiteSpace(query.Site) ? query.Text : $"{query.Text} site:{query.Site}";
        var form = new Dictionary<string, string> { ["q"] = text };
        if (query.Recency is { } recency) form["df"] = recency[..1];

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://html.duckduckgo.com/html/") { Content = new FormUrlEncodedContent(form) };
        request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
        using var response = await _http.SendAsync(request, cancellationToken);
        string html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || html.Contains("anomaly-modal", StringComparison.Ordinal))
            throw new WebSearchException("DuckDuckGo refused the automated search. For reliable search set WEB_SEARCH_PROVIDER=google " +
                                         "with GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID.");

        return ParseResults(html, query.MaxResults);
    }

    internal static List<WebSearchResult> ParseResults(string html, int count)
    {
        var results = new List<WebSearchResult>();
        // Each organic result is a <div class="result ..."> block; ads carry "result--ad" on that opening tag,
        // so each block is sliced to include it.
        var starts = ResultBlock().Matches(html).Select(m => m.Index).Append(html.Length).ToList();
        for (int i = 0; i < starts.Count - 1; i++)
        {
            string block = html[starts[i]..starts[i + 1]];
            if (block.Contains("result--ad", StringComparison.Ordinal)) continue;

            var link = ResultLink().Match(block);
            if (!link.Success) continue;

            string url = DecodeRedirectUrl(WebUtility.HtmlDecode(link.Groups["href"].Value));
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;

            var snippet = ResultSnippet().Match(block);
            results.Add(new WebSearchResult(WebTools.CleanInline(link.Groups["title"].Value), url,
                snippet.Success ? WebTools.CleanInline(snippet.Groups["text"].Value) : ""));
            if (results.Count == count) break;
        }
        return results;
    }

    // Older/alternate markup links through //duckduckgo.com/l/?uddg=<encoded target>.
    private static string DecodeRedirectUrl(string href)
    {
        if (href.StartsWith("//")) href = "https:" + href;
        if (Uri.TryCreate(href, UriKind.Absolute, out var uri) && uri.Host.EndsWith("duckduckgo.com") && uri.AbsolutePath == "/l/")
        {
            var target = uri.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2))
                .FirstOrDefault(p => p.Length == 2 && p[0] == "uddg");
            if (target is not null) return Uri.UnescapeDataString(target[1]);
        }
        return href;
    }

    [GeneratedRegex(@"<div[^>]*class=""[^""]*\bresult\b[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex ResultBlock();

    [GeneratedRegex(@"<a[^>]*class=""[^""]*\bresult__a\b[^""]*""[^>]*href=""(?<href>[^""]+)""[^>]*>(?<title>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ResultLink();

    [GeneratedRegex(@"class=""[^""]*\bresult__snippet\b[^""]*""[^>]*>(?<text>.*?)</(?:a|div|td)>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ResultSnippet();
}
