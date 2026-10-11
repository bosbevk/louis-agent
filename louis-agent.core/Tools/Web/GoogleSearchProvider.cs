namespace LouisAgent.Core.Tools.Web;

using System.Net;
using System.Text.Json;

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
