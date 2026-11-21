using LouisAgent.Diagrams.Engine.Svg.Paints;

namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks;

/// <summary>
/// What a cards picture and a sequence both wear (F134.9, F134.10, Louis's pick "Marquee" for each): the picture's
/// small title, the soft light, the glass with the accent along its top, the pill with its code, a node on a
/// lit line, and the faint rule over a key. A flow and a nested picture wear its title and its light too
/// (F134.12).
/// </summary>
/// <remarks>
/// It was the cards' own until a second kind took the look. Then what the two share moved here, as what a flow and
/// a nested picture share is <see cref="PictureLook"/>'s, so neither can drift from the other in it. A kind's
/// own style comes after it and adds only what is its own. The accent is the palette's violet running to its
/// blue: in this look neither tone says what a box is. The fades are paints the look fills with by id and whose
/// stops it colours. Each is named here once, with how it runs, and a drawing asks for it by that name.
/// </remarks>
internal static class MarqueeLook
{
    /// <summary>The ids of the shared fades: the light, the glass, and the accent along a panel's top.</summary>
    public const string LightPaint = "mrq-light", GlassPaint = "mrq-glass", EdgePaint = "mrq-edge";

    /// <summary>Halfway along a fade's run: where the light is its second colour, and the accent fullest.</summary>
    private const double Halfway = 0.5;

    /// <summary>The soft light behind a picture's top: out from its middle, by its second colour, to nothing.</summary>
    public static readonly Fade LightFade = new(LightPaint, PaintRun.Out, 0, Halfway, 1);

    /// <summary>The glass of a panel: down it, from how much shows at its top to how much at its bottom.</summary>
    public static readonly Fade GlassFade = new(GlassPaint, PaintRun.Down, 0, 1);

    /// <summary>The accent along a panel's top: across it, from nothing to its fullest halfway, and back.</summary>
    public static readonly Fade EdgeFade = new(EdgePaint, PaintRun.Across, 0, Halfway, 1);

    /// <summary>
    /// On a light page: how strong the light is at its middle and halfway out, the colour of the glass and how
    /// much of it shows at a panel's top and bottom, and a panel's hairline. The glass and the hairline have
    /// enough of their dark blue for a card to read as a card on white (F134.12, Louis: "all whites overall can
    /// be a tad darker").
    /// </summary>
    private const string Light =
        "--mrq-glow: .2; --mrq-glow-far: .07; --mrq-sheet: #1b2a4a; --mrq-sheet-top: .06; " +
        "--mrq-sheet-end: .035; --mrq-hair: rgba(27, 42, 74, .18);";

    /// <summary>The same on a dark page, where the glass is white and the light stronger.</summary>
    private const string Dark =
        "--mrq-glow: .34; --mrq-glow-far: .1; --mrq-sheet: #ffffff; --mrq-sheet-top: .07; " +
        "--mrq-sheet-end: .02; --mrq-hair: rgba(255, 255, 255, .1);";

    /// <summary>
    /// The style. It is the same for every theme, since its colours are the theme's own variables.
    /// </summary>
    public static readonly string Rules =
        $$"""

          svg { {{Light}} }
          {{PageScheme.Rules(Light, Dark)}}
          .mrq-title { fill: var(--text); font: 600 13px var(--pic-sans); }
          .mrq-light { fill: url(#{{LightPaint}}); }
          .{{LightPaint}}-0 { stop-color: var(--violet-line); stop-opacity: var(--mrq-glow); }
          .{{LightPaint}}-1 { stop-color: var(--blue-line); stop-opacity: var(--mrq-glow-far); }
          .{{LightPaint}}-2 { stop-color: var(--blue-line); stop-opacity: 0; }
          .mrq-glass { fill: url(#{{GlassPaint}}); stroke: var(--mrq-hair); stroke-width: 1; }
          .{{GlassPaint}}-0 { stop-color: var(--mrq-sheet); stop-opacity: var(--mrq-sheet-top); }
          .{{GlassPaint}}-1 { stop-color: var(--mrq-sheet); stop-opacity: var(--mrq-sheet-end); }
          .mrq-edge { fill: url(#{{EdgePaint}}); }
          .{{EdgePaint}}-0 { stop-color: var(--violet-line); stop-opacity: 0; }
          .{{EdgePaint}}-1 { stop-color: var(--violet-line); }
          .{{EdgePaint}}-2 { stop-color: var(--violet-line); stop-opacity: 0; }
          .mrq-rule { fill: url(#{{EdgePaint}}); opacity: .7; }
          .mrq-pill { fill: none; stroke: var(--violet-line); stroke-width: 1; stroke-opacity: .6; }
          .mrq-code { fill: var(--violet-ink); font: 11px var(--mono); }
          .mrq-node { fill: var(--ground); stroke: var(--violet-line); stroke-width: 1.5; }
          """;
}
