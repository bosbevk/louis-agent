namespace LouisAgent.Core.Tools.Web;

using System.Net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;

/// <summary>
/// Picks the search engine from configuration: WEB_SEARCH_PROVIDER = tavily | google | searxng | duckduckgo. When unset,
/// Tavily is used if TAVILY_API_KEY is set, else Google if GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID are both
/// set, else SearXNG if SEARXNG_URL is (F32: the one we run, for development and benchmarks; a Tavily key keeps the sold
/// endpoint on the licensed API), otherwise keyless DuckDuckGo. Misconfiguration never fails startup; searches report
/// what is missing instead.
/// </summary>
public static class WebSearchProviderFactory
{
    public static IReadOnlyList<string> SupportedProviders { get; } = ["tavily", "google", "searxng", "duckduckgo"];

    public static IWebSearchProvider Create(AgentOptions options, HttpClient? http = null)
    {
        bool hasTavily = !string.IsNullOrWhiteSpace(options.TavilyApiKey);
        bool hasGoogle = !string.IsNullOrWhiteSpace(options.GoogleSearchApiKey) && !string.IsNullOrWhiteSpace(options.GoogleSearchEngineId);
        Uri? searxng = Uri.TryCreate(options.SearxngUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;
        string choice = (options.WebSearchProvider ?? "").Trim().ToLowerInvariant();

        return choice switch
        {
            "" when hasTavily => new TavilySearchProvider(options.TavilyApiKey!, http),
            "" when hasGoogle => new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http),
            "" when searxng is not null => new SearxngSearchProvider(searxng, http),
            "" => new DuckDuckGoSearchProvider(http),
            "tavily" => hasTavily
                ? new TavilySearchProvider(options.TavilyApiKey!, http)
                : new UnavailableSearchProvider("Tavily", MessageText.Format(ToolMessage.TavilyNotConfigured)),
            "google" => hasGoogle
                ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http)
                : new UnavailableSearchProvider("Google", MessageText.Format(ToolMessage.GoogleNotConfigured)),
            "searxng" => searxng is not null
                ? new SearxngSearchProvider(searxng, http)
                : new UnavailableSearchProvider("SearXNG", MessageText.Format(ToolMessage.SearxngNotConfigured)),
            "duckduckgo" or "ddg" => new DuckDuckGoSearchProvider(http),
            _ => new UnavailableSearchProvider(choice, MessageText.Format(ToolMessage.UnknownSearchProvider, new { Provider = choice, Supported = string.Join(", ", SupportedProviders) })),
        };
    }
}
