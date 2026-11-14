using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks;

/// <summary>
/// The look a picture wears when it is drawn in the new style (F134.7, shared since F134.8), as the style its SVG
/// carries beside the theme's: panels with a little body and a hairline edge, names in a sans face, and lines whose
/// dots march the way the arrow points.
/// </summary>
/// <remarks>
/// One look under every kind that wears it (Louis: "try a new style fancier than we have", then of the flow: "the
/// same as the component diagram"). A kind adds only what is its own, as <see cref="ComponentsStyle"/> adds a
/// component's pins and pill, so two kinds can't drift apart in what they share. A panel is filled with a little
/// of the page's ink, the same wherever it stands, so a panel inside a panel is a step deeper with no rule for
/// its depth, and on a white page a box four deep is not white (F134.12, Louis: "composite diagrams white is too
/// white"). The colours stay the theme's tones, so a tone means here what it means in every picture, and no
/// gradient mixes two. Everything that moves stops for a reader who asked for less motion.
/// </remarks>
internal static class PictureLook
{
    /// <summary>A panel's body and its hairline, and the marching dots, on a light page.</summary>
    private const string Light =
        "--pic-body: rgba(27, 42, 74, .065); --pic-hair: rgba(27, 42, 74, .2); " +
        "--pic-march: rgba(255, 255, 255, .95);";

    /// <summary>The same on a dark page, where a panel is a little of white and each depth a step lighter.</summary>
    private const string Dark =
        "--pic-body: rgba(255, 255, 255, .045); --pic-hair: rgba(255, 255, 255, .1); " +
        "--pic-march: rgba(255, 255, 255, .75);";

    /// <summary>The style, for a theme's tones.</summary>
    public static string Of(SvgTheme theme) =>
        $$"""

          svg {
            {{Light}}
            --pic-sans: "Segoe UI", system-ui, -apple-system, Roboto, "Helvetica Neue", Arial, sans-serif;
          }
          @media (prefers-color-scheme: dark) {
            svg { {{Dark}} }
          }
          .pic-edge { stroke: var(--quiet); }
          .pic-tint { fill: var(--chip); }
          .pic-fill { fill: var(--quiet); }
          .pic-ink { fill: var(--text); }
          {{Tones(theme)}}
          .pic-panel { fill: var(--pic-body); stroke: var(--pic-hair); stroke-width: 1; }
          .pic-group {
            fill: var(--quiet); font: 600 10px var(--mono); letter-spacing: 1.4px; text-transform: uppercase;
          }
          .pic-name { fill: var(--text); font: 700 14px var(--pic-sans); letter-spacing: -.1px; }
          .pic-note { fill: var(--quiet); font: 11.5px var(--pic-sans); }
          .pic-means { fill: var(--text); font: 12px var(--pic-sans); }
          .pic-word { fill: var(--quiet); font: 500 10px var(--mono); }
          .pic-port { fill: var(--ground); stroke-width: 1.25; }
          .pic-line { fill: none; stroke-width: 2; stroke-linecap: round; }
          .pic-line.reads { stroke-dasharray: 3 5; }
          .pic-march {
            fill: none; stroke: var(--pic-march); stroke-width: 2; stroke-linecap: round; stroke-dasharray: 1 13;
            animation: pic-march 1.1s linear infinite;
          }
          @keyframes pic-march { to { stroke-dashoffset: -14; } }
          @media (prefers-reduced-motion: reduce) {
            .pic-march { display: none; }
          }
          """;

    /// <summary>
    /// Four rules a tone: its line, its own tint, its solid fill and its ink, for whatever wears it.
    /// </summary>
    private static string Tones(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire()).Select(name =>
            $".pic-edge.tone-{name} {{ stroke: var(--{name}-line); }}\n" +
            $".pic-tint.tone-{name} {{ fill: var(--{name}-fill); }}\n" +
            $".pic-fill.tone-{name} {{ fill: var(--{name}-line); }}\n" +
            $".pic-ink.tone-{name} {{ fill: var(--{name}-ink); }}"));
}
