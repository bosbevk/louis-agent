using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Site.Addresses;

namespace LouisAgent.Web.Components.Widget;

/// <summary>
/// The guide's places as addresses (F118, ADR 0107, Louis: "the agent sections must have an url in the framework,
/// otherwise the agents won't work optimally"): what the guide reads of the page names every section, card and page of
/// a paged section by its own address, and what it hands its page tool is an address read back into the page and the
/// place. One rule for the guide, the index and the address bar.
/// </summary>
public static class GuidePlaces
{
    /// <summary>
    /// The page as the guide reads it, every area named by its address: <c>levels</c> on the identities page becomes
    /// <c>/identities/levels</c>, a paged section's <c>parts/2</c> becomes <c>/how-it-works/parts/2</c>.
    /// </summary>
    public static Page Addressed(Page page, SiteAddresses addresses, string pageAddress) =>
        page with
        {
            Url = pageAddress,
            Areas = page.Areas.Select(area => area with { Id = AddressOf(area.Id, addresses, pageAddress) }).ToList(),
            InView = page.InView is { } inView ? inView with { Id = AddressOf(inView.Id, addresses, pageAddress) } : null,
        };

    /// <summary>
    /// Where a page tool's address goes: the page, and the place on it as the page's script names it (<c>levels</c>,
    /// <c>parts/2</c>), none for the page itself; nothing for an address that names no page of the site.
    /// </summary>
    public static (string Page, string? Place)? Target(string address, SiteAddresses addresses)
    {
        if (addresses.Read(address) is not { } read) return null;
        // The page itself is its first section: showing it lights the top of the page.
        string section = read.Section ?? SiteAddresses.Top;
        return (read.Page, read.Turn is { } turn ? $"{section}/{turn}" : section);
    }

    /// <summary>
    /// One place's address as the page's script names it: <c>levels</c>, <c>parts/2</c>; the first section, <c>top</c>,
    /// is the page itself, and its pages follow the page's address.
    /// </summary>
    public static string AddressOf(string place, SiteAddresses addresses, string pageAddress)
    {
        int slash = place.IndexOf('/');
        if (slash < 0) return addresses.Address(pageAddress, place);
        string section = place[..slash];
        return int.TryParse(place[(slash + 1)..], out int turn)
            ? addresses.Address(pageAddress, section, turn)
            : addresses.Address(pageAddress, section);
    }
}
