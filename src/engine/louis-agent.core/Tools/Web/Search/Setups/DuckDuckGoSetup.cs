using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>
/// Searches through DuckDuckGo. It needs no key, so it's always there as the last fallback.
/// </summary>
/// <remarks>
/// Strategy. It needs nothing, so it is the engine of last resort and the list always ends with a ready one. That is why it has no stand-in.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal sealed class DuckDuckGoSetup : ISearchProviderSetup
{
    /// <summary>This is the DuckDuckGo one.</summary>
    public WebSearchProviderKind Kind => WebSearchProviderKind.DuckDuckGo;

    /// <summary>Nothing to set, so it is always ready.</summary>
    public bool IsReady(AgentOptions options) => true;

    /// <summary>Builds the DuckDuckGo provider. It can't be unset, so there is no stand-in.</summary>
    public IWebSearchProvider Create(AgentOptions options, HttpClient? http) => new DuckDuckGoSearchProvider(http);
}
