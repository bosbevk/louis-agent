using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Engine.Svg.Theme.Pages;

/// <summary>
/// A picture's colours for each of our sites' themes, light and dark, worn when it is drawn into a page (F160.1): on a
/// flat page flat's own, on a glass page glass's, so a picture looks like the page around it.
/// </summary>
/// <remarks>
/// A palette per theme in the engine (Louis's pick C of three, over reading the theme's names and over light and dark
/// alone): the engine keeps its colours in one place it owns, as it does for light and dark, and the docs' tool can
/// draw a picture without the framework's web. Only a page's copy carries these rules, so a picture as a file, in the
/// docs, stays byte for byte as it was. Glass wears the engine's own palette and panels, made for that look; flat its
/// own, from flat's two sheets: panels its solid grey with its firmer line, no body of ink.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
internal static class PagePalettes
{
    /// <summary>
    /// A flat panel on a light page: flat's pane grey and its firmer grey line, and no light or glass sheen behind or
    /// on a panel, since flat has no lights (FX182).
    /// </summary>
    private const string FlatPanelsLight =
        "--pic-body: #dcdee8; --pic-hair: #a7aaba; --pic-march: rgba(255, 255, 255, .95); " +
        "--mrq-glow: 0; --mrq-glow-far: 0; --mrq-sheet-top: 0; --mrq-sheet-end: 0; --mrq-hair: #a7aaba;";

    /// <summary>A flat panel on a dark page: flat's near-black pane and its dark grey line, and no light either.</summary>
    private const string FlatPanelsDark =
        "--pic-body: #101013; --pic-hair: #2b2b33; --pic-march: rgba(255, 255, 255, .75); " +
        "--mrq-glow: 0; --mrq-glow-far: 0; --mrq-sheet-top: 0; --mrq-sheet-end: 0; --mrq-hair: #2b2b33;";

    /// <summary>The rules a page's copy of a picture carries: each theme's colours, light and dark.</summary>
    public static string Rules =>
        PageScheme.ThemeRules(PageScheme.Flat,
            SvgStyle.Variables(SvgTheme.Flat.Light) + " " + FlatPanelsLight,
            SvgStyle.Variables(SvgTheme.Flat.Dark) + " " + FlatPanelsDark) + "\n" +
        PageScheme.ThemeRules(PageScheme.Glass,
            SvgStyle.Variables(SvgTheme.Site.Light) + " " + PictureLook.Light,
            SvgStyle.Variables(SvgTheme.Site.Dark) + " " + PictureLook.Dark);
}
