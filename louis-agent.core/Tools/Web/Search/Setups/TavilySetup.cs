using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>Strategy: Tavily's setup. Ready with <c>TAVILY_API_KEY</c>; without it <see cref="Create"/> returns its Null Object, an unavailable provider that says to set the key.</summary>
internal sealed class TavilySetup : ISearchProviderSetup
{
    /// <summary>Tavily.</summary>
    public WebSearchProviderKind Kind => WebSearchProviderKind.Tavily;

    /// <summary>True when the Tavily API key is set and not blank.</summary>
    public bool IsReady(AgentOptions options) => !string.IsNullOrWhiteSpace(options.TavilyApiKey);

    /// <summary>The Tavily provider when ready; otherwise the Null Object that says the key is missing.</summary>
    public IWebSearchProvider Create(AgentOptions options, HttpClient? http) =>
        IsReady(options)
            ? new TavilySearchProvider(options.TavilyApiKey!, http)
            : new UnavailableSearchProvider("Tavily", MessageText.Format(WebToolMessage.TavilyNotConfigured));
}
