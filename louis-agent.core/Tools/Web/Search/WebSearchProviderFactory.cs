namespace LouisAgent.Core.Tools.Web.Search;

using System.Net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;
using LouisAgent.Choices;

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

    /// <summary>The engine <paramref name="options"/> select: the one named, else the first whose settings are set; null when the name is unknown.</summary>
    public static WebSearchProviderKind? Resolve(AgentOptions options)
    {
        if (!SettingChoice.TryParse(options.WebSearchProvider, out WebSearchProviderKind? choice, Aliases)) return null;
        if (choice is not null) return choice;
        if (HasTavily(options)) return WebSearchProviderKind.Tavily;
        if (HasGoogle(options)) return WebSearchProviderKind.Google;
        return SearxngOf(options) is not null ? WebSearchProviderKind.Searxng : WebSearchProviderKind.DuckDuckGo;
    }

    private static bool HasTavily(AgentOptions options) => !string.IsNullOrWhiteSpace(options.TavilyApiKey);

    private static bool HasGoogle(AgentOptions options) =>
        !string.IsNullOrWhiteSpace(options.GoogleSearchApiKey) && !string.IsNullOrWhiteSpace(options.GoogleSearchEngineId);

    private static Uri? SearxngOf(AgentOptions options) => Uri.TryCreate(options.SearxngUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;

    public static IWebSearchProvider Create(AgentOptions options, HttpClient? http = null)
    {
        bool hasTavily = HasTavily(options);
        bool hasGoogle = HasGoogle(options);
        Uri? searxng = SearxngOf(options);
        if (Resolve(options) is not { } choice)
        {
            string named = options.WebSearchProvider!.Trim();
            return new UnavailableSearchProvider(named, MessageText.Format(WebToolMessage.UnknownSearchProvider,
                new { Provider = named, Supported = string.Join(", ", SupportedProviders) }));
        }

        return choice switch
        {
            WebSearchProviderKind.Tavily => hasTavily
                ? new TavilySearchProvider(options.TavilyApiKey!, http)
                : new UnavailableSearchProvider("Tavily", MessageText.Format(WebToolMessage.TavilyNotConfigured)),
            WebSearchProviderKind.Google => hasGoogle
                ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http)
                : new UnavailableSearchProvider("Google", MessageText.Format(WebToolMessage.GoogleNotConfigured)),
            WebSearchProviderKind.Searxng => searxng is not null
                ? new SearxngSearchProvider(searxng, http)
                : new UnavailableSearchProvider("SearXNG", MessageText.Format(WebToolMessage.SearxngNotConfigured)),
            WebSearchProviderKind.DuckDuckGo => new DuckDuckGoSearchProvider(http),
            _ => throw new ArgumentOutOfRangeException(nameof(options), choice, null),
        };
    }
}
