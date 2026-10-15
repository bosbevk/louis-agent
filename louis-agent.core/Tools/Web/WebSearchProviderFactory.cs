namespace LouisAgent.Core.Tools.Web;

using System.Net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;

/// <summary>
/// Picks the search engine from configuration: WEB_SEARCH_PROVIDER = tavily | google | duckduckgo. When unset, Tavily
/// is used if TAVILY_API_KEY is set, else Google if GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID are both set,
/// otherwise keyless DuckDuckGo. Misconfiguration never fails startup; searches report what is missing instead.
/// </summary>
public static class WebSearchProviderFactory
{
    public static IReadOnlyList<string> SupportedProviders { get; } = ["tavily", "google", "duckduckgo"];

    public static IWebSearchProvider Create(AgentOptions options, HttpClient? http = null)
    {
        bool hasTavily = !string.IsNullOrWhiteSpace(options.TavilyApiKey);
        bool hasGoogle = !string.IsNullOrWhiteSpace(options.GoogleSearchApiKey) && !string.IsNullOrWhiteSpace(options.GoogleSearchEngineId);
        string choice = (options.WebSearchProvider ?? "").Trim().ToLowerInvariant();

        return choice switch
        {
            "" when hasTavily => new TavilySearchProvider(options.TavilyApiKey!, http),
            "" => hasGoogle ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http) : new DuckDuckGoSearchProvider(http),
            "tavily" => hasTavily
                ? new TavilySearchProvider(options.TavilyApiKey!, http)
                : new UnavailableSearchProvider("Tavily", MessageText.Format(ToolMessage.TavilyNotConfigured)),
            "google" => hasGoogle
                ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http)
                : new UnavailableSearchProvider("Google", MessageText.Format(ToolMessage.GoogleNotConfigured)),
            "duckduckgo" or "ddg" => new DuckDuckGoSearchProvider(http),
            _ => new UnavailableSearchProvider(choice, MessageText.Format(ToolMessage.UnknownSearchProvider, new { Provider = choice, Supported = string.Join(", ", SupportedProviders) })),
        };
    }
}
