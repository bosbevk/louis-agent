
using System.Net;

namespace LouisAgent.Composition.Web.Search;
/// <summary>
/// A web search engine behind the web search tool. Implement this to plug in another engine
/// (Bing, SearXNG, an internal search, a test fake) and pass it to the engine.
/// </summary>
public interface IWebSearchProvider
{
    /// <summary>Shown to the model, e.g. "Google".</summary>
    string Name { get; }

    /// <summary>
    /// Returns up to <see cref="WebSearchQuery.MaxResults"/> results. Throw <see cref="WebSearchException"/> for
    /// problems the user can act on (bad key, quota, blocked); its message is shown as is.
    /// </summary>
    Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default);
}
