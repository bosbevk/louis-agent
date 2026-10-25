namespace LouisAgent.Core.Tools.Web.Rendering.Providers;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering;

/// <summary>
/// Tavily's Extract endpoint as the page renderer (F25 step 7; Louis's choice over a hosted reader and a headless
/// browser in the container): <c>POST https://api.tavily.com/extract</c> with the key as a bearer header, one URL per
/// call, <c>extract_depth: advanced</c> because JavaScript sites (Pick n Pay's product pages) are why the tool
/// exists; markdown back. Two credits per five pages at this depth, from the same monthly credits as the search.
/// A page Tavily couldn't open comes back under <c>failed_results</c> with HTTP 200, so both lists are read.
/// See https://docs.tavily.com/documentation/api-reference/endpoint/extract.
/// </summary>
public sealed class TavilyPageRenderer(string apiKey, HttpClient? http = null) : IPageRenderer
{
    private static readonly Uri Endpoint = new("https://api.tavily.com/extract");

    private readonly HttpClient _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

    public string Name => "Tavily";

    public async Task<RenderedPage> RenderAsync(Uri url, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object>
        {
            ["urls"] = new[] { url.ToString() },
            ["extract_depth"] = "advanced",
            ["format"] = "markdown",
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await _http.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string detail = TavilyError(json) ?? response.ReasonPhrase ?? "";
            throw new PageRenderException(response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => MessageText.Format(WebToolMessage.TavilyRejected, new { Detail = detail }),
                HttpStatusCode.TooManyRequests or (HttpStatusCode)432 or (HttpStatusCode)433 => MessageText.Format(WebToolMessage.TavilyLimit, new { Detail = detail }),
                _ => MessageText.Format(WebToolMessage.RenderFailed, new { Url = url, Reason = $"HTTP {(int)response.StatusCode} {detail}" }),
            });
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0)
        {
            var first = results[0];
            string text = first.TryGetProperty("raw_content", out var c) ? c.GetString() ?? "" : "";
            string reported = first.TryGetProperty("url", out var u) ? u.GetString() ?? url.ToString() : url.ToString();
            return new RenderedPage(reported, text.ReplaceLineEndings("\n").Trim());
        }

        string reason = root.TryGetProperty("failed_results", out var failed) && failed.ValueKind == JsonValueKind.Array && failed.GetArrayLength() > 0
            && failed[0].TryGetProperty("error", out var e) ? e.GetString() ?? "" : MessageText.Format(WebToolMessage.RenderNoResult);
        throw new PageRenderException(MessageText.Format(WebToolMessage.RenderFailed, new { Url = url, Reason = reason }));
    }

    /// <summary>Tavily's error body: <c>{"detail": {"error": "…"}}</c>, or <c>{"detail": "…"}</c> from its framework.</summary>
    internal static string? TavilyError(string json)
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
