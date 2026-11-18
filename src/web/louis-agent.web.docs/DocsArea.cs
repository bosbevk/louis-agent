using LouisAgent.Web.Components.Areas;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Docs;

/// <summary>
/// The docs' address as an area a site names (F161.1): <c>.Docs(address)</c> says where every doc link on its pages
/// leads.
/// </summary>
/// <remarks>
/// An extension on the framework's <see cref="SiteAreasBuilder"/>, from this library, since the framework's web
/// can't name the docs' parts (dependencies point one way), as the accounts' area does.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public static class DocsArea
{
    /// <summary>The docs at <paramref name="address"/>, for every doc link on the site's pages.</summary>
    public static SiteAreasBuilder Docs(this SiteAreasBuilder areas, DocsAddress address) =>
        areas.Add(new SiteArea("docs", services => services.AddSingleton(address), []));
}
