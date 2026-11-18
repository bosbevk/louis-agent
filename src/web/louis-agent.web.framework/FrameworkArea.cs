using LouisAgent.Web.Components.Areas;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework;

/// <summary>
/// The framework's pages as an area a site names (F161.1, Louis: "the framework must be a package like accounts"):
/// <c>.Framework()</c> brings the pages to the site's router, and their contents in reading order to the docs
/// layout's menu and page turns.
/// </summary>
/// <remarks>
/// An extension on the framework's <see cref="SiteAreasBuilder"/>, from this library, since the framework's web
/// can't name its own pages' package (dependencies point one way), as the accounts' area does.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public static class FrameworkArea
{
    /// <summary>The framework's pages, on the site's router, with their contents.</summary>
    public static SiteAreasBuilder Framework(this SiteAreasBuilder areas) =>
        areas.Add(new SiteArea("framework", services => services.AddSingleton(FrameworkContents.All),
            [FrameworkLibrary.Assembly]));
}
