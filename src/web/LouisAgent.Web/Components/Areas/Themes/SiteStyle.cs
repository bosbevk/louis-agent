using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Areas.Themes;

/// <summary>
/// The two styles every theme comes in (F151.1), read once with <see cref="SettingChoice.TryParse{T}"/> from the word
/// the browser keeps and written back with <c>.Wire()</c>, as the page's <c>data-style</c> says it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums, not strings</seealso>
public enum SiteStyle
{
    /// <summary>A light page with dark words: the style a first visit gets.</summary>
    [WireName("light")] Light,

    /// <summary>A dark page with light words.</summary>
    [WireName("dark")] Dark
}
