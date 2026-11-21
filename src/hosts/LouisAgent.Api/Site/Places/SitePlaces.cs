using LouisAgent.Api.Site.Rendering;
using LouisAgent.Composition.Sites;

namespace LouisAgent.Api.Site.Places;

/// <summary>
/// Every place on a site's pages, read from the pages themselves (F127.7, Louis: "instead of these site maps is there
/// not a better way"): each page is rendered once, the first time anyone asks, as the prerender renders it for search,
/// and its sections, cards and pieces are kept for the host's life. A page that fails to render is left out.
/// </summary>
/// <remarks>
/// A search index built from the real pages, the pattern site assistants use (Algolia DocSearch, Mintlify): records
/// by section, each with its address, never a list kept by hand. Lazy Load (Fowler, PoEAA): rendering every page costs
/// a moment, so it waits for the first question rather than slowing the host's start.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public sealed class SitePlaces : ISitePlaces
{
    private readonly Lazy<Task<IReadOnlyList<SitePlace>>> _places;

    /// <summary>The index over the site's pages, rendered with the prerenderer that serves them.</summary>
    public SitePlaces(SiteRoutes routes, PagePrerenderer prerenderer) =>
        _places = new Lazy<Task<IReadOnlyList<SitePlace>>>(() => ReadAsync(routes, prerenderer));

    /// <inheritdoc/>
    public bool HasSite => true;

    /// <inheritdoc/>
    public Task<IReadOnlyList<SitePlace>> AllAsync(CancellationToken cancellationToken = default) =>
        _places.Value.WaitAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SitePlace>> FindAsync(string words, int most,
        CancellationToken cancellationToken = default) =>
        PlaceSearch.Find(await AllAsync(cancellationToken), words, most);

    /// <summary>Renders each page and reads its places, page after page.</summary>
    private static async Task<IReadOnlyList<SitePlace>> ReadAsync(SiteRoutes routes, PagePrerenderer prerenderer)
    {
        var places = new List<SitePlace>();
        foreach (var (route, page) in routes.Pages)
        {
            // Rendered at its own address on a local host: the address only fills the page's links, never fetched.
            var address = new Uri(new Uri("http://localhost/"), route.TrimStart('/'));
            var rendered = await prerenderer.RenderAsync(page, address);
            if (rendered is null) continue;
            places.AddRange(PlaceReader.Read(rendered.Body, routes.PlacesUnder(route)));
        }

        return places;
    }
}
