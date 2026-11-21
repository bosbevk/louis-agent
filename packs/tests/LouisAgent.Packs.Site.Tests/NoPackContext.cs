using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Pricing;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Search;

namespace LouisAgent.Packs.Site.Tests;

/// <summary>
/// A host that offers the site pack nothing: no site, and no web, which the pack never asks for. Building its tools
/// needs only the site's places (F127.7), which fall back to none.
/// </summary>
internal sealed class NoPackContext : IPackContext
{
    public string WorkspaceRoot => throw new NotSupportedException();

    public IWebBrowser? Browser => null;

    public IWebSearchProvider WebSearch => throw new NotSupportedException();

    public IPageRenderer Renderer => throw new NotSupportedException();

    public ICurrencyRates? CurrencyRates => null;

    public IPageFetcher NewFetcher() => throw new NotSupportedException();
}
