using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Testing.Sites;

/// <summary>
/// The look a site's page opens in before any script runs (F151.1): the theme and the style its <c>index.html</c>
/// writes on <c>&lt;html&gt;</c>. A site's test compares what its page writes with what its areas say it should, the
/// default theme they give and the default style, so the two can't part. Each site's own tests do it, since only
/// they can call the site's areas.
/// </summary>
/// <param name="Theme">The theme's id, or none.</param>
/// <param name="Style">The style's word, or none.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
public sealed record IndexLook(string? Theme, string? Style)
{
    /// <summary>What opens the page's root element.</summary>
    private const string Root = "<html";

    /// <summary>What the site's <c>index.html</c> writes on its <c>&lt;html&gt;</c>.</summary>
    public static IndexLook Of(string siteFolder)
    {
        string page = File.ReadAllText(Path.Combine(siteFolder, "wwwroot", "index.html"));
        int open = page.IndexOf(Root, StringComparison.Ordinal);
        string tag = page[open..(page.IndexOf('>', open) + 1)];
        return new IndexLook(Attribute(tag, SiteLook.ThemeAttribute), Attribute(tag, SiteLook.StyleAttribute));
    }

    /// <summary>What a site's page should write, given the areas it names: their default theme, and light.</summary>
    public static IndexLook For(Action<SiteAreasBuilder> areas)
    {
        var named = new ServiceCollection().AddSiteAreas(areas).BuildServiceProvider().GetRequiredService<SiteAreas>();
        return new IndexLook(named.DefaultTheme?.Id, SiteLook.DefaultStyle.Wire());
    }

    /// <summary>
    /// One attribute's value on the tag, or none. The space before the name keeps a longer name that ends in it,
    /// such as <c>x-data-theme</c>, from being read as it.
    /// </summary>
    private static string? Attribute(string tag, string name)
    {
        string start = $" {name}=\"";
        int at = tag.IndexOf(start, StringComparison.Ordinal);
        if (at < 0) return null;

        int value = at + start.Length;
        return tag[value..tag.IndexOf('"', value)];
    }
}
