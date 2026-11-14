namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a cards picture's look adds to the shared ones (F134.9, Louis's pick of three designs, "Marquee"): the lit
/// rail the parts hang from and its lines, a headline with its badge, a glass card's badge and name, and the
/// quiet card. A card's text is <see cref="PictureLook"/>'s.
/// </summary>
/// <remarks>
/// A card has no tone, so the colour here is the look's own: one accent, the palette's violet running to its blue.
/// What a sequence wears too, the small title, the light, the glass, the pill and a node, is
/// <see cref="MarqueeLook"/>'s, and what every picture in the new look wears is <see cref="PictureLook"/>'s. The
/// rail's fade and the badge's are paints: the style fills a part with one by its id and colours its stops, and
/// the drawing defines the paint under the same id.
/// </remarks>
internal static class CardsStyle
{
    /// <summary>The ids of the cards' own fades: the rail, and a headline's badge.</summary>
    public const string RailPaint = "crd-rail", BadgePaint = "crd-badge";

    /// <summary>
    /// The style. It is the same for every theme, since its colours are the theme's own variables.
    /// </summary>
    public const string Rules =
        $$"""

          .crd-rail { fill: url(#{{RailPaint}}); }
          .crd-rail-glow { fill: url(#{{RailPaint}}); opacity: .5; filter: blur(5px); }
          .{{RailPaint}}-0 { stop-color: var(--violet-line); stop-opacity: 0; }
          .{{RailPaint}}-1 { stop-color: var(--violet-line); }
          .{{RailPaint}}-2 { stop-color: var(--blue-line); }
          .{{RailPaint}}-3 { stop-color: var(--blue-line); stop-opacity: 0; }
          .crd-line { fill: none; stroke: var(--violet-line); stroke-width: 1.5; stroke-opacity: .85; }
          .crd-badge { fill: url(#{{BadgePaint}}); }
          .{{BadgePaint}}-0 { stop-color: var(--violet-line); }
          .{{BadgePaint}}-1 { stop-color: var(--blue-line); }
          .crd-badge-icon {
            fill: none; stroke: #ffffff; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round;
          }
          .crd-headline { fill: var(--text); font: 700 30px var(--pic-sans); letter-spacing: -.4px; }
          .crd-heading { fill: var(--text); font: 700 22px var(--pic-sans); letter-spacing: -.3px; }
          .crd-lede { fill: var(--quiet); font: 13px var(--pic-sans); }
          .crd-tint { fill: var(--violet-line); fill-opacity: .16; }
          .crd-icon {
            fill: none; stroke: var(--violet-ink); stroke-width: 1.8; stroke-linecap: round; stroke-linejoin: round;
          }
          .crd-icon.quiet { stroke: var(--quiet); }
          .crd-name { fill: var(--text); font: 600 15px var(--pic-sans); }
          .crd-name.quiet { font-size: 14px; }
          .crd-quiet {
            fill: var(--mrq-sheet); fill-opacity: .018; stroke: var(--mrq-sheet); stroke-opacity: .08;
            stroke-width: 1;
          }
          """;
}
