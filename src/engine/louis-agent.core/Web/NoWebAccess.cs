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
    public static readonly NoWebAccess Instance = new();

    private static readonly string Reason = MessageText.Format(WebToolMessage.WebAccessNotSetUp);

    private NoWebAccess()
    {
    }

    public IWebSearchProvider Search { get; } = new UnavailableSearchProvider("none", Reason);

    public IWebBrowser? Browser => null;

    public IPageRenderer Renderer { get; } = new UnavailablePageRenderer(Reason);

    public IPageFetcher NewFetcher() => new UnavailablePageFetcher(Reason);
}
