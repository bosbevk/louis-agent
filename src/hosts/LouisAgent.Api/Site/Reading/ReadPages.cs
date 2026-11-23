using System.Collections.Concurrent;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.Composition.Sites;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Site.Reading;

/// <summary>
/// The site's pages, each read as a message carries it the first time it is asked for and kept for the host's life,
/// so a run with three repeats renders a page once and nobody can make the host render on every call.
/// </summary>
/// <remarks>
/// Each page is kept as a lazy read, as the site's places index keeps its places, so two asks at once still render it
/// once. The focus comes from the site's own rule, read from the services the site's pages are rendered with.
/// </remarks>
internal sealed class ReadPages(SiteRoutes routes, PagePrerenderer prerenderer, SiteFocuses? focuses)
{
    private readonly ConcurrentDictionary<string, Lazy<Task<PageRequest?>>> _read = new(StringComparer.Ordinal);

    /// <summary>The pages of these routes, with the focus rule the site's services register, if any.</summary>
    public static ReadPages Create(SiteRoutes routes, PagePrerenderer prerenderer,
        Action<IServiceCollection> siteServices)
    {
        var services = new ServiceCollection();
        siteServices(services);
        using var provider = services.BuildServiceProvider();
        return new ReadPages(routes, prerenderer, provider.GetService<SiteFocuses>());
    }

    /// <summary>The page that holds this address, read; none when there is no such page or it won't render.</summary>
    public Task<PageRequest?> FindAsync(string address)
    {
        if (routes.PageFor(address) is not { } found) return Task.FromResult<PageRequest?>(null);
        var read = _read.GetOrAdd(found.Route,
            _ => new Lazy<Task<PageRequest?>>(() => ReadAsync(found.Route, found.Page)));
        return read.Value;
    }

    /// <summary>Renders the page at its own address on a local host, and reads it.</summary>
    private async Task<PageRequest?> ReadAsync(string route, Type page)
    {
        // The address only fills the page's links, never fetched, as the site's places index renders.
        var address = new Uri(new Uri("http://localhost/"), route.TrimStart('/'));
        var rendered = await prerenderer.RenderAsync(page, address);
        return rendered is null
            ? null
            : PageReader.Read(rendered, route, routes.PlacesUnder(route), focuses?.Of(route));
    }
}
