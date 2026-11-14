using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Svg.Theme.Looks;

/// <summary>
/// The look a picture wears when it is drawn in the new style (F134.7, shared since F134.8), as the style its SVG
/// carries beside the theme's: a ground of faint dots, the picture's own title, panels that get lighter the deeper
/// they nest, each tinted in its tone and lifted by a soft shadow, names in a sans face, and lines whose dots march
/// the way the arrow points.
/// </summary>
/// <remarks>
/// One look under every kind that wears it (Louis: "try a new style fancier than we have", then of the flow: "the
/// same as the component diagram"). A kind adds only what is its own, as <see cref="ComponentsStyle"/> adds a
/// component's pins and pill, so two kinds can't drift apart in what they share. The colours stay the theme's
/// tones, so a tone means here what it means in every picture, and no gradient mixes two. Everything that moves
/// stops for a reader who asked for less motion.
/// </remarks>
internal static class PictureLook
{
    /// <summary>
    /// How deep the look tells panels apart. A panel nested deeper than this looks like one at this depth.
    /// </summary>
    public const int Deepest = 5;

    /// <summary>The panels by depth, the shadow, the ground's dots and the marching dots, on a light page.</summary>
    private const string Light =
        "--pic-0: #ffffff; --pic-1: #f4f6f9; --pic-2: #ffffff; --pic-3: #f4f6f9; --pic-4: #ffffff; " +
        "--pic-5: #f4f6f9; --pic-lift: rgba(27, 42, 74, .14); --pic-dot: rgba(15, 23, 42, .075); " +
        "--pic-march: rgba(255, 255, 255, .95); --pic-sheen: rgba(255, 255, 255, .9);";

    /// <summary>The same on a dark page, where each depth is a little lighter than the one that holds it.</summary>
    private const string Dark =
        "--pic-0: #11141a; --pic-1: #151920; --pic-2: #191e26; --pic-3: #1d232c; --pic-4: #212832; " +
        "--pic-5: #262e39; --pic-lift: rgba(0, 0, 0, .5); --pic-dot: rgba(255, 255, 255, .055); " +
        "--pic-march: rgba(255, 255, 255, .75); --pic-sheen: rgba(255, 255, 255, .09);";

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
          .pic-dots {
            fill: none; stroke: var(--pic-dot); stroke-width: 2.2; stroke-linecap: round; stroke-dasharray: 0 22;
          }
          .pic-edge { stroke: var(--quiet); }
          .pic-tint { fill: var(--chip); }
          .pic-fill { fill: var(--quiet); }
          .pic-ink { fill: var(--text); }
          {{Tones(theme)}}
          .pic-panel {
            fill: color-mix(in srgb, var(--pic-tone, var(--pic-depth)) 42%, var(--pic-depth));
            stroke-width: 1; stroke-opacity: .5; filter: drop-shadow(0 8px 9px var(--pic-lift));
          }
          {{Depths()}}
          .pic-sheen { fill: none; stroke: var(--pic-sheen); stroke-width: 1; stroke-linecap: round; }
          .pic-swatch { stroke-width: 1; }
          .pic-title { fill: var(--text); font: 700 21px var(--pic-sans); letter-spacing: -.2px; }
          .pic-group {
            fill: var(--quiet); font: 600 10px var(--mono); letter-spacing: 1.4px; text-transform: uppercase;
          }
          .pic-name { fill: var(--text); font: 600 15px var(--pic-sans); }
          .pic-note { fill: var(--quiet); font: 11.5px var(--pic-sans); }
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
    /// A rule a depth: the colour a panel at that depth starts from, before its tone tints it.
    /// </summary>
    private static string Depths() => string.Join("\n", Enumerable.Range(0, Deepest + 1)
        .Select(depth => $".pic-panel.depth-{depth} {{ --pic-depth: var(--pic-{depth}); }}"));

    /// <summary>
    /// Five rules a tone: its line, the tint of a panel in it, its own tint, its solid fill and its ink, for
    /// whatever wears it.
    /// </summary>
    private static string Tones(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire()).Select(name =>
            $".pic-edge.tone-{name} {{ stroke: var(--{name}-line); }}\n" +
            $".pic-panel.tone-{name} {{ --pic-tone: var(--{name}-fill); }}\n" +
            $".pic-tint.tone-{name} {{ fill: var(--{name}-fill); }}\n" +
            $".pic-fill.tone-{name} {{ fill: var(--{name}-line); }}\n" +
            $".pic-ink.tone-{name} {{ fill: var(--{name}-ink); }}"));
}
