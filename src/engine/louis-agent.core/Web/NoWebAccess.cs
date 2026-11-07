using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Messages;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Search;

namespace LouisAgent.Core.Web;

/// <summary>
/// The web on a host with no web library: no browser, and a search provider, a renderer and a fetcher that each say
/// web access isn't set up, so a web tool answers the model with why instead of failing.
/// </summary>
/// <remarks>
/// Null Object: the engine factory holds one of these when a host hands it no web access and asks it the same
/// questions, so no toolset has an "if there is a web" branch.
/// </remarks>
public sealed class NoWebAccess : IWebAccess
{
    /// <summary>The one instance: it holds nothing of its own.</summary>
    public static readonly NoWebAccess Instance = new();

    private static readonly string Reason = MessageText.Format(WebToolMessage.WebAccessNotSetUp);

    private NoWebAccess()
    {
    }

    /// <summary>A search that answers with the reason.</summary>
    public IWebSearchProvider Search { get; } = new UnavailableSearchProvider("none", Reason);

    /// <summary>No browser, so a toolset that needs one is unavailable.</summary>
    public IWebBrowser? Browser => null;

    /// <summary>A renderer that fails every page with the reason.</summary>
    public IPageRenderer Renderer { get; } = new UnavailablePageRenderer(Reason);

    /// <summary>A fetcher that fails every fetch with the reason.</summary>
    public IPageFetcher NewFetcher() => new UnavailablePageFetcher(Reason);
}
