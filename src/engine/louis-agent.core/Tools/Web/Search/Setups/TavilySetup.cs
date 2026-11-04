using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>
/// Searches the web through Tavily, the paid API the sold endpoint uses. Without the key, the model is told to set `TAVILY_API_KEY`.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. The sold endpoint reaches Tavily with a key; without one, the first search says what to set instead of start-up failing.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0023-copyleft-service-beside-us.md">ADR 0023: a copyleft service may run beside us</seealso>
internal sealed class TavilySetup : ISearchProviderSetup
{
    /// <summary>This is the Tavily one.</summary>
    public WebSearchProviderKind Kind => WebSearchProviderKind.Tavily;

    /// <summary>We need the Tavily key.</summary>
    public bool IsReady(AgentOptions options) => !string.IsNullOrWhiteSpace(options.TavilyApiKey);

    /// <summary>Builds the Tavily provider, or the one that says to set the key.</summary>
    public IWebSearchProvider Create(AgentOptions options, HttpClient? http) =>
        IsReady(options)
            ? new TavilySearchProvider(options.TavilyApiKey!, http)
            : new UnavailableSearchProvider("Tavily", MessageText.Format(WebToolMessage.TavilyNotConfigured));
}
