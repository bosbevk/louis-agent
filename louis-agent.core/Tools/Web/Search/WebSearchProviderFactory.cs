namespace LouisAgent.Core.Tools.Web.Search;

using System.Net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;
using LouisAgent.Core.Tools.Web.Search.Setups;
using LouisAgent.Choices;

/// <summary>
/// Picks the search engine from configuration: WEB_SEARCH_PROVIDER = tavily | google | searxng | duckduckgo. When unset,
/// Tavily is used if TAVILY_API_KEY is set, else Google if GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID are both
/// set, else SearXNG if SEARXNG_URL is (F32: the one we run, for development and benchmarks; a Tavily key keeps the sold
/// endpoint on the licensed API), otherwise keyless DuckDuckGo. Misconfiguration never fails startup; searches report
/// what is missing instead. Strategy + Null Object (rule 11): each engine is a setup that knows if it is ready and how to build itself, and a setup that is
/// not ready hands back its unavailable provider, so the factory only picks and never checks settings.
/// </summary>
public static class WebSearchProviderFactory
{
    /// <summary>The names <c>WEB_SEARCH_PROVIDER</c> takes, from <see cref="WebSearchProviderKind"/>.</summary>
    public static IReadOnlyList<string> SupportedProviders { get; } = SettingChoice.Names<WebSearchProviderKind>();

    private static readonly IReadOnlyDictionary<string, WebSearchProviderKind> Aliases =
        new Dictionary<string, WebSearchProviderKind> { ["ddg"] = WebSearchProviderKind.DuckDuckGo };

    /// <summary>Strategy: one setup per engine, in fallback order (Tavily, Google, SearXNG, DuckDuckGo). With no engine named, the first ready setup is the one used.</summary>
    internal static IReadOnlyList<ISearchProviderSetup> Setups { get; } = [new TavilySetup(), new GoogleSetup(), new SearxngSetup(), new DuckDuckGoSetup()];

    /// <summary>
    /// The engine <paramref name="options"/> select: the one named, else the first ready one; null when the name is unknown. Named and not ready still gives that
    /// engine, whose <see cref="Create"/> is then its Null Object.
    /// </summary>
    public static WebSearchProviderKind? Resolve(AgentOptions options) => Pick(options)?.Kind;

    /// <summary>Strategy: the setup of the engine named, else the first ready one in <see cref="Setups"/> (DuckDuckGo, last, is always ready); null when the name is unknown.</summary>
    private static ISearchProviderSetup? Pick(AgentOptions options)
    {
        if (!SettingChoice.TryParse(options.WebSearchProvider, out WebSearchProviderKind? named, Aliases)) return null;
        return named is { } kind ? Setups.Single(s => s.Kind == kind) : Setups.First(s => s.IsReady(options));
    }

    /// <summary>The search provider for <paramref name="options"/>: the picked setup's, which is its Null Object when its settings are missing; an unknown name gets the unavailable provider that lists the supported names.</summary>
    public static IWebSearchProvider Create(AgentOptions options, HttpClient? http = null)
    {
        if (Pick(options) is { } setup) return setup.Create(options, http);
        string named = options.WebSearchProvider!.Trim();
        return new UnavailableSearchProvider(named, MessageText.Format(WebToolMessage.UnknownSearchProvider,
            new { Provider = named, Supported = string.Join(", ", SupportedProviders) }));
    }
}
