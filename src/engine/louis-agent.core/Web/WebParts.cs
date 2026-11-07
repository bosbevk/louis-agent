using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Search;

namespace LouisAgent.Core.Web;

/// <summary>
/// Web access from the parts given, for a test or a host that brings its own: the search provider, a browser if
/// any, a renderer, a way to make a fetcher. What isn't given is <see cref="NoWebAccess"/>'s, which says web access
/// isn't set up.
/// </summary>
/// <param name="Search">The search provider the tools get.</param>
/// <param name="Browser">The browser, if any.</param>
/// <param name="Renderer">The page renderer; none means the "not set up" one.</param>
/// <param name="Fetcher">How a fetcher is made for a call; none means the "not set up" one.</param>
public sealed record WebParts(
    IWebSearchProvider Search,
    IWebBrowser? Browser = null,
    IPageRenderer? Renderer = null,
    Func<IPageFetcher>? Fetcher = null) : IWebAccess
{
    /// <summary>The renderer given, else the one that says web access isn't set up.</summary>
    IPageRenderer IWebAccess.Renderer => Renderer ?? NoWebAccess.Instance.Renderer;

    /// <summary>A fetcher the way given, else one that says web access isn't set up.</summary>
    public IPageFetcher NewFetcher() => Fetcher?.Invoke() ?? NoWebAccess.Instance.NewFetcher();
}
