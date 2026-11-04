using System.Net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;
using LouisAgent.Core.Tools.Web.Search.Setups;
using LouisAgent.Choices;

namespace LouisAgent.Core.Tools.Web.Search;

/// <summary>
/// Picks the search engine you configured, or else the first one that's set up: Tavily, then Google, then SearXNG, then DuckDuckGo. A missing setting never stops start-up; the search just tells the model what to set.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. The engines are a list of setups instead of a switch on the name, so adding an engine is one new class.
/// An engine that isn't set up gives a provider that explains what's missing, so nothing has to check for null.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class WebSearchProviderFactory
{
    /// <summary>The engine names you may put in `WEB_SEARCH_PROVIDER`, for the error message.</summary>
    public static IReadOnlyList<string> SupportedProviders { get; } = SettingChoice.Names<WebSearchProviderKind>();

    /// <summary>Other names for an engine: `ddg` means DuckDuckGo.</summary>
    private static readonly IReadOnlyDictionary<string, WebSearchProviderKind> Aliases =
        new Dictionary<string, WebSearchProviderKind> { ["ddg"] = WebSearchProviderKind.DuckDuckGo };

    /// <summary>The engines, in the order we fall back to them.</summary>
    internal static IReadOnlyList<ISearchProviderSetup> Setups { get; } =
        [new TavilySetup(), new GoogleSetup(), new SearxngSetup(), new DuckDuckGoSetup()];

    /// <summary>Tells you which engine these settings pick: the one named, or else the first that's set up. Nothing if the name is unknown.</summary>
    public static WebSearchProviderKind? Resolve(AgentOptions options) => Pick(options)?.Kind;

    /// <summary>Whether the engine these settings pick is actually set up.</summary>
    public static bool IsReady(AgentOptions options) => Pick(options)?.IsReady(options) ?? false;

    /// <summary>Finds the setup of the engine you named, or else the first one that's set up.</summary>
    private static ISearchProviderSetup? Pick(AgentOptions options)
    {
        // A name we don't know picks nothing.
        if (!SettingChoice.TryParse(options.WebSearchProvider, out WebSearchProviderKind? named, Aliases)) return null;

        // A name we know gets that engine, set up or not. No name gets the first that is set up; DuckDuckGo, last, always is.
        return named is { } kind ? Setups.Single(s => s.Kind == kind) : Setups.First(s => s.IsReady(options));
    }

    /// <summary>Gives you the search provider these settings pick. If it isn't set up, or the name is unknown, you get one that tells the model what's wrong.</summary>
    public static IWebSearchProvider Create(AgentOptions options, HttpClient? http = null)
    {
        // The setup builds its provider, or its "not set up" stand-in.
        if (Pick(options) is { } setup) return setup.Create(options, http);

        // Nothing was picked, so the name isn't one we know: say what was given and which names we do support.
        string named = options.WebSearchProvider!.Trim();
        return new UnavailableSearchProvider(named, MessageText.Format(WebToolMessage.UnknownSearchProvider,
            new { Provider = named, Supported = string.Join(", ", SupportedProviders) }));
    }
}
