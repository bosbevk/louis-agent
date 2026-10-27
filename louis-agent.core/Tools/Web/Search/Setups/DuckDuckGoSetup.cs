using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>Strategy: DuckDuckGo's setup. It needs no key, so it is always ready and <see cref="Create"/> never has a Null Object to hand back; it is the last fallback.</summary>
internal sealed class DuckDuckGoSetup : ISearchProviderSetup
{
    /// <summary>DuckDuckGo.</summary>
    public WebSearchProviderKind Kind => WebSearchProviderKind.DuckDuckGo;

    /// <summary>Always true: the keyless engine needs no settings.</summary>
    public bool IsReady(AgentOptions options) => true;

    /// <summary>The DuckDuckGo provider.</summary>
    public IWebSearchProvider Create(AgentOptions options, HttpClient? http) => new DuckDuckGoSearchProvider(http);
}
