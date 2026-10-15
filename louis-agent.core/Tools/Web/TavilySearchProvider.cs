namespace LouisAgent.Core.Tools.Web;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Core.Messages;

/// <summary>
/// Tavily's search API (F25 step 6; Louis's choice over Google, whose Custom Search API ends on 2027-01-01): a search
/// built for agents, 1,000 free searches a month. <c>POST https://api.tavily.com/search</c> with the key as a bearer
/// header, never in the body; <see cref="WebSearchQuery.Site"/> becomes <c>include_domains</c> and
/// <see cref="WebSearchQuery.Recency"/> <c>time_range</c> (the same words: day, week, month, year). Each result's
/// <c>content</c> is the page's most relevant text, longer than a snippet, so the model often needs no fetch.
/// See https://docs.tavily.com/documentation/api-reference/endpoint/search.
/// </summary>
public sealed class TavilySearchProvider(string apiKey, HttpClient? http = null) : IWebSearchProvider
{
    private static readonly Uri Endpoint = new("https://api.tavily.com/search");

    private readonly HttpClient _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    public string Name => "Tavily";

    public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object>
        {
            ["query"] = query.Text,
            ["max_results"] = Math.Clamp(query.MaxResults, 1, 20),
            ["search_depth"] = "basic",
        };
        if (!string.IsNullOrWhiteSpace(query.Site)) body["include_domains"] = new[] { query.Site.Trim() };
        if (query.Recency is { } recency) body["time_range"] = recency;

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await _http.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string detail = TryReadError(json) ?? response.ReasonPhrase ?? "";
            throw new WebSearchException(response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => MessageText.Format(ToolMessage.TavilyRejected, new { Detail = detail }),
                // 429 is the rate limit; 432 and 433 are Tavily's own codes for a plan's usage limit.
                HttpStatusCode.TooManyRequests or (HttpStatusCode)432 or (HttpStatusCode)433 =>
                    MessageText.Format(ToolMessage.TavilyLimit, new { Detail = detail }),
                _ => MessageText.Format(ToolMessage.TavilyFailed, new { Status = (int)response.StatusCode, Detail = detail }),
            });
        }

        return ParseResults(json, query.MaxResults);
    }

    internal static List<WebSearchResult> ParseResults(string json, int count)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array) return [];

        return results.EnumerateArray()
            .Select(r => new WebSearchResult(
                WebTools.CleanInline(r.TryGetProperty("title", out var t) ? t.GetString() : ""),
                r.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                WebTools.CleanInline(r.TryGetProperty("content", out var c) ? c.GetString() : "")))
            .Where(r => r.Url.Length > 0)
            .Take(count)
            .ToList();
    }

    /// <summary>Tavily's error body: <c>{"detail": {"error": "…"}}</c>, or <c>{"detail": "…"}</c> from its framework.</summary>
    private static string? TryReadError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("detail", out var detail)) return null;
            return detail.ValueKind switch
            {
                JsonValueKind.String => detail.GetString(),
                JsonValueKind.Object when detail.TryGetProperty("error", out var error) => error.GetString(),
                _ => detail.GetRawText(),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
