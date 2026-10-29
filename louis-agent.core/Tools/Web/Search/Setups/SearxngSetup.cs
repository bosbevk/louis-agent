using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>
/// Searches through SearXNG, the engine we run ourselves for development and benchmarks. If we don't know where it runs, the model is told to set `SEARXNG_URL`.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. SearXNG only exists where an address is configured, so elsewhere the first search says what to set instead of failing.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0023-copyleft-service-beside-us.md">ADR 0023: a copyleft service may run beside us</seealso>
internal sealed class SearxngSetup : ISearchProviderSetup
{
    /// <summary>This is the SearXNG one.</summary>
    public WebSearchProviderKind Kind => WebSearchProviderKind.Searxng;

    /// <summary>We need to know where SearXNG runs.</summary>
    public bool IsReady(AgentOptions options) => AddressOf(options) is not null;

    /// <summary>Builds the SearXNG provider, or the one that says to set the address.</summary>
    public IWebSearchProvider Create(AgentOptions options, HttpClient? http) =>
        AddressOf(options) is { } address
            ? new SearxngSearchProvider(address, http)
            : new UnavailableSearchProvider("SearXNG", MessageText.Format(WebToolMessage.SearxngNotConfigured));

    /// <summary>Reads the address, or nothing if it isn't a real URL.</summary>
    private static Uri? AddressOf(AgentOptions options) =>
        Uri.TryCreate(options.SearxngUrl?.Trim(), UriKind.Absolute, out var url) ? url : null;
}
