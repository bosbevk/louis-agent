using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Engine.Svg.Theme.Pages;

/// <summary>
/// A picture's colours for each of our sites' themes, light and dark, worn when it is drawn into a page (F164.1): on a
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

    /// <summary>A flat card on a light page: smoke white, and flat's own line, the one its palette draws.</summary>
    private static string FlatCardsLight =>
        SmokeWhite + $" {CardLines(SvgTheme.Flat.Light)} --pic-march: rgba(255, 255, 255, .95);";

    /// <summary>A flat card on a dark page: flat's near-black pane, and flat's own line, as in the light.</summary>
    private static string FlatCardsDark =>
        $"--pic-body: #141418; {CardLines(SvgTheme.Flat.Dark)} --pic-march: rgba(255, 255, 255, .75);";

    /// <summary>
    /// A card's two lines take the palette's own line, so a card never drifts from the picture it sits in (F169.1).
    /// </summary>
    private static string CardLines(SvgPalette palette) =>
        $"--pic-hair: {palette.Line}; --mrq-hair: {palette.Line};";

    /// <summary>
    /// A card on a page, in every theme: glass's sheened card and the quiet card's faint wash are both the card's own
    /// fill under its line, so the cards are the only things filled and every card can be seen (Louis: "fix the
    /// borders or fill it with gray cannot see the boxes").
    /// </summary>
    private const string Cards =
        ":scope .mrq-glass, :scope .crd-quiet " +
        "{ fill: var(--pic-body); fill-opacity: 1; stroke: var(--pic-hair); stroke-opacity: 1; }";

    /// <summary>
    /// On a flat page a picture has no glow and no light, since flat has no shading: the glow behind a rail and a
    /// beam, the light behind the top, a panel's edge of light and a ticket's light are hidden (F169.4).
    /// </summary>
    /// <remarks>
    /// Louis's pick 3, "A: no fade, no blur, the six colours kept", over making every tone the one violet as well,
    /// which would leave a box's kind to its shape and words. So only the shading goes, and the tones stay.
    /// </remarks>
    private static string FlatLights => PageScheme.PartRule(PageScheme.Flat,
        ".crd-rail-glow, .seq-beam-glow, .mrq-light, .mrq-edge, .ticket-light", "display: none;");

    /// <summary>
    /// On a flat page a picture is filled with colours only (F169.4, the same pick): a rail, a beam going or coming
    /// back and a badge with no tone of its own in the picture's violet line, in place of their fades; a lifeline and
    /// a title's rule in the picture's hairline at full strength; and a key's swatch filled as a card is. A badge with
    /// a tone keeps it, since the six tones say what each box is.
    /// </summary>
    private static string FlatFills => string.Join("\n",
        PageScheme.PartRule(PageScheme.Flat, ".crd-rail, .seq-beam, .crd-badge:not([class*=\"tone-\"])",
            "fill: var(--violet-line);"),
        PageScheme.PartRule(PageScheme.Flat, ".seq-life, .mrq-rule", "fill: var(--pic-hair); opacity: 1;"),
        PageScheme.PartRule(PageScheme.Flat, ".seq-swatch",
            "fill: var(--pic-body); fill-opacity: 1; stroke: var(--pic-hair); stroke-opacity: 1;"));

    /// <summary>
    /// The rules a page's copy of a picture carries: each theme's colours, light and dark, its cards, and flat's
    /// parts.
    /// </summary>
    public static string Rules =>
        PageScheme.ThemeRules(PageScheme.Flat,
            SvgStyle.Variables(SvgTheme.Flat.Light) + " " + FlatCardsLight + " " + Transparent,
            SvgStyle.Variables(SvgTheme.Flat.Dark) + " " + FlatCardsDark + " " + Transparent) + "\n" +
        PageScheme.ThemeRules(PageScheme.Glass,
            SvgStyle.Variables(SvgTheme.Site.Light) + " " + PictureLook.Light + " " + SmokeWhite + " " + Transparent,
            SvgStyle.Variables(SvgTheme.Site.Dark) + " " + PictureLook.Dark + " " + Transparent) + "\n" + Cards +
        "\n" + FlatLights + "\n" + FlatFills;
}
