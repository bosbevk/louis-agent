using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>Strategy: SearXNG's setup (the one we run ourselves, ADR 0023). Ready with a valid <c>SEARXNG_URL</c>; otherwise <see cref="Create"/> returns its Null Object, an unavailable provider that says to set it.</summary>
internal sealed class SearxngSetup : ISearchProviderSetup
{
    /// <summary>SearXNG.</summary>
    public WebSearchProviderKind Kind => WebSearchProviderKind.Searxng;

    /// <summary>True when the SearXNG address is set and is an absolute URL.</summary>
    public bool IsReady(AgentOptions options) => AddressOf(options) is not null;

    /// <summary>The SearXNG provider when ready; otherwise the Null Object that says the address is missing.</summary>
    public IWebSearchProvider Create(AgentOptions options, HttpClient? http) =>
        AddressOf(options) is { } address
            ? new SearxngSearchProvider(address, http)
            : new UnavailableSearchProvider("SearXNG", MessageText.Format(WebToolMessage.SearxngNotConfigured));

    /// <summary>The configured SearXNG address as a URL, or null when it is missing or not an absolute URL.</summary>
    private static Uri? AddressOf(AgentOptions options) => Uri.TryCreate(options.SearxngUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;
}
