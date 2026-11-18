using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Engine.Svg.Theme.Pages;

/// <summary>
/// A picture's colours for each of our sites' themes, light and dark, worn when it is drawn into a page (F161.1): on a
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
    /// On a page, the picture's own background is transparent in every theme (Louis: "can we make all backgrounds
    /// transparent only the cards can have a background colour"): no light behind its top and no sheen on a panel.
    /// </summary>
    private const string Transparent = "--mrq-glow: 0; --mrq-glow-far: 0; --mrq-sheet-top: 0; --mrq-sheet-end: 0;";

    /// <summary>A card's fill on a light page, in both themes (Louis: "light is smoke white").</summary>
    private const string SmokeWhite = "--pic-body: #f5f5f5;";

    /// <summary>A flat card on a light page: smoke white, and flat's firm grey line.</summary>
    private const string FlatCardsLight =
        SmokeWhite + " --pic-hair: #a7aaba; --mrq-hair: #a7aaba; --pic-march: rgba(255, 255, 255, .95);";

    /// <summary>A flat card on a dark page: flat's near-black pane, and its dark grey line.</summary>
    private const string FlatCardsDark =
        "--pic-body: #141418; --pic-hair: #2b2b33; --mrq-hair: #2b2b33; --pic-march: rgba(255, 255, 255, .75);";

    /// <summary>
    /// A card on a page, in every theme: glass's sheened card and the quiet card's faint wash are both the card's own
    /// fill under its line, so the cards are the only things filled and every card can be seen (Louis: "fix the
    /// borders or fill it with gray cannot see the boxes").
    /// </summary>
    private const string Cards =
        ":scope .mrq-glass, :scope .crd-quiet " +
        "{ fill: var(--pic-body); fill-opacity: 1; stroke: var(--pic-hair); stroke-opacity: 1; }";

    /// <summary>The rules a page's copy of a picture carries: each theme's colours, light and dark, and its cards.</summary>
    public static string Rules =>
        PageScheme.ThemeRules(PageScheme.Flat,
            SvgStyle.Variables(SvgTheme.Flat.Light) + " " + FlatCardsLight + " " + Transparent,
            SvgStyle.Variables(SvgTheme.Flat.Dark) + " " + FlatCardsDark + " " + Transparent) + "\n" +
        PageScheme.ThemeRules(PageScheme.Glass,
            SvgStyle.Variables(SvgTheme.Site.Light) + " " + PictureLook.Light + " " + SmokeWhite + " " + Transparent,
            SvgStyle.Variables(SvgTheme.Site.Dark) + " " + PictureLook.Dark + " " + Transparent) + "\n" + Cards;
}
