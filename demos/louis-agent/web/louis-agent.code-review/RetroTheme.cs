using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.CodeReview;

/// <summary>
/// The retro theme, this app's own (F151.5): its id and name, and the area the app names to wear it,
/// <c>.Retro()</c>. The look is its two sheets of values, a terminal in the dark and the same on paper. It lives
/// here and not with the framework's themes because it is this app's and no other site's.
/// </summary>
/// <remarks>
/// An extension on the framework's <see cref="SiteAreasBuilder"/>, the door glass and flat come through, so a site's
/// own theme joins the same way as the framework's and the switch offers it with no code of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
public static class RetroTheme
{
    /// <summary>What the area is called among the app's areas.</summary>
    public const string AreaName = "theme-retro";

    /// <summary>The theme: <c>retro</c> in the page and its stylesheet, Retro on the switch.</summary>
    public static readonly SiteTheme Theme = new("retro", "Retro");

    /// <summary>The retro theme, for the app to wear beside the framework's.</summary>
    public static SiteAreasBuilder Retro(this SiteAreasBuilder areas)
    {
        var retro = new SiteArea(AreaName, _ => { }, [], Theme: Theme);
        return areas.Add(retro);
    }
}
