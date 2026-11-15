namespace LouisAgent.Web.Components.Areas.Themes;

/// <summary>
/// What the C# side and the browser side agree on about a page's look (F151.1): the two words the browser remembers
/// the visitor's pick under, the two attributes on the page's <c>&lt;html&gt;</c> that say which theme and style are
/// on, and the style a first visit gets. <c>first-paint.js</c> and the switch use the same names, and a test reads
/// them from here so the two sides cannot drift.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
public static class SiteLook
{
    /// <summary>The word the browser keeps the visitor's theme under.</summary>
    public const string ThemeKey = "site.theme";

    /// <summary>The word the browser keeps the visitor's style under.</summary>
    public const string StyleKey = "site.style";

    /// <summary>The attribute on the page's <c>&lt;html&gt;</c> that names the theme that is on.</summary>
    public const string ThemeAttribute = "data-theme";

    /// <summary>The attribute on the page's <c>&lt;html&gt;</c> that names the style that is on.</summary>
    public const string StyleAttribute = "data-style";

    /// <summary>The style a first visit gets, whatever the device's own setting is.</summary>
    public static SiteStyle DefaultStyle => SiteStyle.Light;
}
