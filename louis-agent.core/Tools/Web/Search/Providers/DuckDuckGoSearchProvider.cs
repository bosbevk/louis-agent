using LouisAgent.Choices;
namespace LouisAgent.Core.Tools.Web.Search.Providers;

using LouisAgent.Core.Tools.Web.Fetching;
using System.Net;
using System.Text.RegularExpressions;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Messages;

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
        if (query.Recency is { } recency) form["df"] = Letter(recency);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://html.duckduckgo.com/html/") { Content = new FormUrlEncodedContent(form) };
        request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
        using var response = await _http.SendAsync(request, cancellationToken);
        string html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || html.Contains("anomaly-modal", StringComparison.Ordinal))
            throw new WebSearchException(MessageText.Format(WebToolMessage.DuckDuckGoRefused));

        return ParseResults(html, query.MaxResults);
    }

    /// <summary>A recency's first letter, as DuckDuckGo's <c>df</c> and Google's <c>dateRestrict</c> write it: d, w, m or y.</summary>
    internal static string Letter(SearchRecency recency) => recency.Wire()[..1];

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
            results.Add(new WebSearchResult(HtmlText.CleanInline(link.Groups["title"].Value), url,
                snippet.Success ? HtmlText.CleanInline(snippet.Groups["text"].Value) : ""));
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
