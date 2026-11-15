using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.Web.Themes.Glass;

/// <summary>
/// The glass theme (F151.2): its id and name, and the area a site names to wear it, <c>.Glass()</c>. The look
/// itself is its sheets of values, one block for each style; this class only tells the site the theme is there. The
/// area registers nothing and brings no pages.
/// </summary>
/// <remarks>
/// An extension on the framework's <see cref="SiteAreasBuilder"/>, from the themes project, since the framework's web
/// can't name a theme (dependencies point one way), as the accounts library adds its own area.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
public static class GlassTheme
{
    /// <summary>What the area is called among a site's areas.</summary>
    public const string AreaName = "theme-glass";

    /// <summary>The theme: <c>glass</c> in the page and its stylesheet, Glass on the switch.</summary>
    public static readonly SiteTheme Theme = new("glass", "Glass");

    /// <summary>The glass theme, for a site to wear; the first theme a site names is its default.</summary>
    public static SiteAreasBuilder Glass(this SiteAreasBuilder areas)
    {
        var glass = new SiteArea(AreaName, _ => { }, [], Theme: Theme);
        return areas.Add(glass);
    }
}
