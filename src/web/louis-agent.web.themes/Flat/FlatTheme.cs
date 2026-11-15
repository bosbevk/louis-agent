using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.Web.Themes.Flat;

/// <summary>
/// The flat theme (F151.2): its id and name, and the area a site names to wear it, <c>.Flat()</c>. The look itself
/// is its sheets of values, one block for each style; this class only tells the site the theme is there. The area
/// registers nothing and brings no pages.
/// </summary>
/// <remarks>
/// An extension on the framework's <see cref="SiteAreasBuilder"/>, from the themes project, since the framework's web
/// can't name a theme (dependencies point one way), as the accounts library adds its own area.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
public static class FlatTheme
{
    /// <summary>What the area is called among a site's areas.</summary>
    public const string AreaName = "theme-flat";

    /// <summary>The theme: <c>flat</c> in the page and its stylesheet, Flat on the switch.</summary>
    public static readonly SiteTheme Theme = new("flat", "Flat");

    /// <summary>The flat theme, for a site to wear; the first theme a site names is its default.</summary>
    public static SiteAreasBuilder Flat(this SiteAreasBuilder areas)
    {
        var flat = new SiteArea(AreaName, _ => { }, [], Theme: Theme);
        return areas.Add(flat);
    }
}
