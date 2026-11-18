using System.Reflection;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LouisAgent.Web.Places;

/// <summary>
/// Every place on a site's pages has its own address (ADR 0107), as an area a site names (F161.1):
/// <c>.Places(home, site)</c> brings the router's catch-all that draws a place's page, and reads the addresses from
/// the pages the site and its other areas serve, the landing's places under <paramref name="home"/>.
/// </summary>
/// <remarks>
/// An extension on the framework's <see cref="SiteAreasBuilder"/>, from this library, as the docs' and the accounts'
/// areas are, so a site names its places once instead of registering its addresses by hand in its program and its
/// host (ADR 0120), and turning on a package of pages gives them their places too. Louis's pick A of three, "A .Places()
/// area", over keeping the hand-registered addresses or bringing the catch-all with every site.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public static class PlacesArea
{
    /// <summary>
    /// The places of the site's own pages, in <paramref name="site"/>, and of the pages its areas bring, the
    /// landing's under <paramref name="home"/>, as in <c>/home/welcome</c>.
    /// </summary>
    public static SiteAreasBuilder Places(this SiteAreasBuilder areas, string home, Assembly site) =>
        // Read when first asked, once every area is named, so the pages of an area named after this one count too.
        areas.Add(new SiteArea("places", services => services.TryAddSingleton(provider =>
                SiteAddresses.From(home, [site, .. provider.GetRequiredService<SiteAreas>().PageAssemblies])),
            [typeof(SectionPage).Assembly]));
}
