namespace LouisAgent.Composition.Sites;

/// <summary>
/// Every place on a website the agent sits on (F127.7): its sections, cards and pieces, each at its own address, so a
/// tool can find one on any page and take the visitor there. The host that serves the site builds it from the site's
/// own pages, so nobody keeps a list by hand.
/// </summary>
/// <remarks>
/// Separated Interface (Fowler, PoEAA), as <see cref="Packs.IPackContext"/>'s other members are: a pack's tool asks
/// this small contract, and the host that renders the pages implements it, so the pack never references a host. A
/// search engine for a site's own pages is the pattern site assistants use (Algolia DocSearch, Mintlify): records by
/// section, each with its address, searched by a tool, rather than a map in the prompt.
/// </remarks>
public interface ISitePlaces
{
    /// <summary>Whether a site's pages are behind it; a host that serves no site has none to find.</summary>
    bool HasSite { get; }

    /// <summary>Every place of every page, in the pages' order and each page's own.</summary>
    Task<IReadOnlyList<SitePlace>> AllAsync(CancellationToken cancellationToken = default);

    /// <summary>The places whose words hold the given ones, the best first, at most <paramref name="most"/>.</summary>
    Task<IReadOnlyList<SitePlace>> FindAsync(string words, int most, CancellationToken cancellationToken = default);
}
