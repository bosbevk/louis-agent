namespace LouisAgent.Core.Tools.Web;

using System.Net;

/// <summary>Stands in for a provider that is selected but not usable, so the agent can tell the user why.</summary>
public sealed class UnavailableSearchProvider(string name, string reason) : IWebSearchProvider
{
    public string Name => name;

    public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default) =>
        throw new WebSearchException(reason);
}
