namespace LouisAgent.Core.Tools.Web.Search;

using System.Net;
using LouisAgent.Core.Tools.Web;

/// <summary>
/// A web search engine behind <see cref="WebTools.WebSearch"/>. Implement this to plug in another engine
/// (Bing, SearXNG, an internal search, a test fake) and pass it to <see cref="AgentEngine"/>.
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
