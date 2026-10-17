namespace LouisAgent.Core.Tools.Web.Search;

using System.Net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;

/// <summary>
/// Picks the search engine from configuration: WEB_SEARCH_PROVIDER = tavily | google | searxng | duckduckgo. When unset,
/// Tavily is used if TAVILY_API_KEY is set, else Google if GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID are both
/// set, else SearXNG if SEARXNG_URL is (F32: the one we run, for development and benchmarks; a Tavily key keeps the sold
/// endpoint on the licensed API), otherwise keyless DuckDuckGo. Misconfiguration never fails startup; searches report
/// what is missing instead.
/// </summary>
public static class WebSearchProviderFactory
{
    /// <summary>The names <c>WEB_SEARCH_PROVIDER</c> takes, from <see cref="WebSearchProviderKind"/>.</summary>
    public static IReadOnlyList<string> SupportedProviders { get; } = SettingChoice.Names<WebSearchProviderKind>();

    private static readonly IReadOnlyDictionary<string, WebSearchProviderKind> Aliases =
        new Dictionary<string, WebSearchProviderKind> { ["ddg"] = WebSearchProviderKind.DuckDuckGo };

    public static IWebSearchProvider Create(AgentOptions options, HttpClient? http = null)
    {
        bool hasTavily = !string.IsNullOrWhiteSpace(options.TavilyApiKey);
        bool hasGoogle = !string.IsNullOrWhiteSpace(options.GoogleSearchApiKey) && !string.IsNullOrWhiteSpace(options.GoogleSearchEngineId);
        Uri? searxng = Uri.TryCreate(options.SearxngUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;
        if (!SettingChoice.TryParse(options.WebSearchProvider, out WebSearchProviderKind? choice, Aliases))
        {
            string named = options.WebSearchProvider!.Trim();
            return new UnavailableSearchProvider(named, MessageText.Format(ToolMessage.UnknownSearchProvider,
                new { Provider = named, Supported = string.Join(", ", SupportedProviders) }));
        }

        return choice switch
        {
            null when hasTavily => new TavilySearchProvider(options.TavilyApiKey!, http),
            null when hasGoogle => new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http),
            null when searxng is not null => new SearxngSearchProvider(searxng, http),
            null => new DuckDuckGoSearchProvider(http),
            WebSearchProviderKind.Tavily => hasTavily
                ? new TavilySearchProvider(options.TavilyApiKey!, http)
                : new UnavailableSearchProvider("Tavily", MessageText.Format(ToolMessage.TavilyNotConfigured)),
            WebSearchProviderKind.Google => hasGoogle
                ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http)
                : new UnavailableSearchProvider("Google", MessageText.Format(ToolMessage.GoogleNotConfigured)),
            WebSearchProviderKind.Searxng => searxng is not null
                ? new SearxngSearchProvider(searxng, http)
                : new UnavailableSearchProvider("SearXNG", MessageText.Format(ToolMessage.SearxngNotConfigured)),
            WebSearchProviderKind.DuckDuckGo => new DuckDuckGoSearchProvider(http),
            _ => throw new ArgumentOutOfRangeException(nameof(options), choice, null),
        };
    }
}
