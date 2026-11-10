using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Addresses;

/// <summary>
/// The frame a place's own address is drawn in (F127.1, Louis: "when you refresh the page it loses the demo
/// container"): the router's last resort draws the page an address names, and this gives it that page's own layout, so
/// <c>/restaurant-demo/the-demo</c> opened afresh is in the showroom, as the click that wrote it was.
/// </summary>
/// <remarks>
/// The router asks the routed page for its layout, and the place page has none of its own, since it draws whichever
/// page the address names. A site's router hands this to its <c>RouteView</c> as the default layout: the router's
/// own public part, so nothing of how Blazor draws a page is copied here.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public static class PlaceLayout
{
    /// <summary>
    /// The layout of the page a place's address names, when that page has one of its own; else the site's default.
    /// </summary>
    public static Type Of(RouteData route, Type fallback, SiteAddresses? addresses)
    {
        if (addresses is null || !typeof(SectionPage).IsAssignableFrom(route.PageType)) return fallback;
        if (route.RouteValues.GetValueOrDefault("Path") is not string path) return fallback;
        if (addresses.Read("/" + path) is not { } read) return fallback;

        return addresses.PageType(read.Page)?.GetCustomAttribute<LayoutAttribute>()?.LayoutType ?? fallback;
    }
}
