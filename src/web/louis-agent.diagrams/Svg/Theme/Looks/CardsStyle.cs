namespace LouisAgent.Diagrams.Svg.Theme.Looks;

/// <summary>
/// What a cards picture's look adds to the shared one (F134.9, Louis's pick of three designs, "Marquee"): the light
/// behind a headline, the lit rail the parts hang from, the glass card and the quiet one, the badge and the pill.
/// </summary>
/// <remarks>
/// A card has no tone, so the colour here is the design's own: one accent, the palette's violet running to its
/// blue. That is the one place a picture mixes two tones in a gradient, since here neither says what a box is.
/// Both are the theme's, so the accent has its light and its dark value as every tone has. The look's fades are
/// paints: the style fills a part with one by its id and colours its stops, and the drawing defines the paint
/// under the same id. Everything a flow and a component diagram wear too is <see cref="PictureLook"/>'s.
/// </remarks>
internal static class CardsStyle
{
    /// <summary>
    /// The ids of the look's fades: the light behind a headline, the rail, a headline's badge, the accent along a
    /// glass card's top, and the glass itself.
    /// </summary>
    public const string LightPaint = "crd-light", RailPaint = "crd-rail", BadgePaint = "crd-badge",
        EdgePaint = "crd-edge", GlassPaint = "crd-glass";

    /// <summary>
    /// On a light page: how strong the light is at its middle and halfway out, the colour of the glass and how
    /// much of it shows at a card's top and bottom, and a card's hairline.
    /// </summary>
    private const string Light =
        "--crd-glow: .2; --crd-glow-far: .07; --crd-sheet: #1b2a4a; --crd-sheet-top: .035; " +
        "--crd-sheet-end: .015; --crd-hair: rgba(27, 42, 74, .13);";

    /// <summary>The same on a dark page, where the glass is white and the light stronger.</summary>
    private const string Dark =
        "--crd-glow: .34; --crd-glow-far: .1; --crd-sheet: #ffffff; --crd-sheet-top: .07; " +
        "--crd-sheet-end: .02; --crd-hair: rgba(255, 255, 255, .1);";

    /// <summary>
    /// The style. It is the same for every theme, since its colours are the theme's own variables.
    /// </summary>
    public const string Rules =
        $$"""

          svg { {{Light}} }
          @media (prefers-color-scheme: dark) {
            svg { {{Dark}} }
          }
          .crd-title { fill: var(--text); font: 600 13px var(--pic-sans); }
          .crd-light { fill: url(#{{LightPaint}}); }
          .{{LightPaint}}-0 { stop-color: var(--violet-line); stop-opacity: var(--crd-glow); }
          .{{LightPaint}}-1 { stop-color: var(--blue-line); stop-opacity: var(--crd-glow-far); }
          .{{LightPaint}}-2 { stop-color: var(--blue-line); stop-opacity: 0; }
          .crd-rail { fill: url(#{{RailPaint}}); }
          .crd-rail-glow { fill: url(#{{RailPaint}}); opacity: .5; filter: blur(5px); }
          .{{RailPaint}}-0 { stop-color: var(--violet-line); stop-opacity: 0; }
          .{{RailPaint}}-1 { stop-color: var(--violet-line); }
          .{{RailPaint}}-2 { stop-color: var(--blue-line); }
          .{{RailPaint}}-3 { stop-color: var(--blue-line); stop-opacity: 0; }
          .crd-line { fill: none; stroke: var(--violet-line); stroke-width: 1.5; stroke-opacity: .85; }
          .crd-node { fill: var(--ground); stroke: var(--violet-line); stroke-width: 1.5; }
          .crd-arrow { fill: var(--violet-line); }
          .crd-badge { fill: url(#{{BadgePaint}}); }
          .{{BadgePaint}}-0 { stop-color: var(--violet-line); }
          .{{BadgePaint}}-1 { stop-color: var(--blue-line); }
          .crd-badge-icon {
            fill: none; stroke: #ffffff; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round;
          }
          .crd-headline { fill: var(--text); font: 700 30px var(--pic-sans); letter-spacing: -.4px; }
          .crd-heading { fill: var(--text); font: 700 22px var(--pic-sans); letter-spacing: -.3px; }
          .crd-lede { fill: var(--quiet); font: 13px var(--pic-sans); }
          .crd-pill { fill: none; stroke: var(--violet-line); stroke-width: 1; stroke-opacity: .6; }
          .crd-code { fill: var(--violet-ink); font: 11px var(--mono); }
          .crd-glass { fill: url(#{{GlassPaint}}); stroke: var(--crd-hair); stroke-width: 1; }
          .{{GlassPaint}}-0 { stop-color: var(--crd-sheet); stop-opacity: var(--crd-sheet-top); }
          .{{GlassPaint}}-1 { stop-color: var(--crd-sheet); stop-opacity: var(--crd-sheet-end); }
          .crd-edge { fill: url(#{{EdgePaint}}); }
          .{{EdgePaint}}-0 { stop-color: var(--violet-line); stop-opacity: 0; }
          .{{EdgePaint}}-1 { stop-color: var(--violet-line); }
          .{{EdgePaint}}-2 { stop-color: var(--violet-line); stop-opacity: 0; }
          .crd-tint { fill: var(--violet-line); fill-opacity: .16; }
          .crd-icon {
            fill: none; stroke: var(--violet-ink); stroke-width: 1.8; stroke-linecap: round; stroke-linejoin: round;
          }
          .crd-icon.quiet { stroke: var(--quiet); }
          .crd-name { fill: var(--text); font: 600 15px var(--pic-sans); }
          .crd-name.quiet { font-size: 14px; }
          .crd-text { fill: var(--quiet); font: 12px var(--pic-sans); }
          .crd-quiet {
            fill: var(--crd-sheet); fill-opacity: .018; stroke: var(--crd-sheet); stroke-opacity: .08;
            stroke-width: 1;
          }
          """;
}
