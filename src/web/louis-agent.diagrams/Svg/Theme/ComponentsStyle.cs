using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Svg.Theme;

/// <summary>
/// The look of a component diagram (F134.7), as the style its SVG carries beside the shared one: panels that get
/// lighter the deeper they nest, each lifted by a soft shadow, names in a sans face, lines whose dots march the way
/// the data flows, and a ground of faint dots.
/// </summary>
/// <remarks>
/// A look of its own inside the frame every docs picture has (Louis: "try a new style fancier than we have"). The
/// colours stay the theme's tones, so a tone means here what it means in every picture, and no gradient mixes two.
/// Everything that moves stops for a reader who asked for less motion.
/// </remarks>
internal static class ComponentsStyle
{
    /// <summary>
    /// How deep the look tells boxes apart. A box nested deeper than this looks like one at this depth.
    /// </summary>
    public const int Deepest = 5;

    /// <summary>The panels by depth, the shadow, the ground's dots and the marching dots, on a light page.</summary>
    private const string Light =
        "--cmp-0: #ffffff; --cmp-1: #f4f6f9; --cmp-2: #ffffff; --cmp-3: #f4f6f9; --cmp-4: #ffffff; " +
        "--cmp-5: #f4f6f9; --cmp-lift: rgba(27, 42, 74, .14); --cmp-dot: rgba(15, 23, 42, .075); " +
        "--cmp-march: rgba(255, 255, 255, .95);";

    /// <summary>The same on a dark page, where each depth is a little lighter than the one that holds it.</summary>
    private const string Dark =
        "--cmp-0: #11141a; --cmp-1: #151920; --cmp-2: #191e26; --cmp-3: #1d232c; --cmp-4: #212832; " +
        "--cmp-5: #262e39; --cmp-lift: rgba(0, 0, 0, .5); --cmp-dot: rgba(255, 255, 255, .055); " +
        "--cmp-march: rgba(255, 255, 255, .75);";

    /// <summary>The style, for a theme's tones.</summary>
    public static string Of(SvgTheme theme) =>
        $$"""

          svg {
            {{Light}}
            --cmp-sans: "Segoe UI", system-ui, -apple-system, Roboto, "Helvetica Neue", Arial, sans-serif;
          }
          @media (prefers-color-scheme: dark) {
            svg { {{Dark}} }
          }
          .cmp-dots {
            fill: none; stroke: var(--cmp-dot); stroke-width: 2.2; stroke-linecap: round; stroke-dasharray: 0 22;
          }
          .cmp-edge { stroke: var(--quiet); }
          .cmp-tint { fill: var(--chip); }
          .cmp-fill { fill: var(--quiet); }
          .cmp-ink { fill: var(--text); }
          {{Tones(theme)}}
          .cmp-panel { stroke-width: 1; stroke-opacity: .5; filter: drop-shadow(0 8px 9px var(--cmp-lift)); }
          {{Depths()}}
          .cmp-ghost { fill: none; stroke-width: 1; stroke-dasharray: 4 4; stroke-opacity: .7; }
          .cmp-name { fill: var(--text); font: 600 15px var(--cmp-sans); }
          .cmp-code-name { fill: var(--text); font: 600 12.5px var(--mono); }
          .cmp-note { fill: var(--quiet); font: 11.5px var(--cmp-sans); }
          .cmp-chip-text, .cmp-pin-text { font: 500 10px var(--mono); }
          .cmp-word { fill: var(--quiet); font: 500 10px var(--mono); }
          .cmp-pin { stroke-width: 1.25; }
          .cmp-pin.up, .cmp-port { fill: var(--ground); }
          .cmp-port { stroke-width: 1.25; }
          .cmp-pill { stroke-width: 1.5; filter: drop-shadow(0 0 8px var(--quiet)); }
          {{Glows(theme)}}
          .cmp-pill-text { fill: var(--text); font: 600 12.5px var(--cmp-sans); }
          .cmp-pulse { animation: cmp-pulse 2.4s ease-in-out infinite; }
          .cmp-line { fill: none; stroke-width: 2; stroke-linecap: round; }
          .cmp-line.reads { stroke-dasharray: 3 5; }
          .cmp-march {
            fill: none; stroke: var(--cmp-march); stroke-width: 2; stroke-linecap: round; stroke-dasharray: 1 13;
            animation: cmp-march 1.1s linear infinite;
          }
          @keyframes cmp-march { to { stroke-dashoffset: -14; } }
          @keyframes cmp-pulse { 50% { opacity: .35; } }
          @media (prefers-reduced-motion: reduce) {
            .cmp-march { display: none; }
            .cmp-pulse { animation: none; }
          }
          """;

    /// <summary>A rule a depth: the panel's fill at that depth.</summary>
    private static string Depths() => string.Join("\n", Enumerable.Range(0, Deepest + 1)
        .Select(depth => $".cmp-panel.depth-{depth} {{ fill: var(--cmp-{depth}); }}"));

    /// <summary>A rule a tone: a pill glows in the colour of the box that holds it.</summary>
    private static string Glows(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire())
        .Select(name => $".cmp-pill.tone-{name} {{ filter: drop-shadow(0 0 8px var(--{name}-line)); }}"));

    /// <summary>Four rules a tone: its line, its tint, its solid fill and its ink, for whatever wears it.</summary>
    private static string Tones(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire()).Select(name =>
            $".cmp-edge.tone-{name} {{ stroke: var(--{name}-line); }}\n" +
            $".cmp-tint.tone-{name} {{ fill: var(--{name}-fill); }}\n" +
            $".cmp-fill.tone-{name} {{ fill: var(--{name}-line); }}\n" +
            $".cmp-ink.tone-{name} {{ fill: var(--{name}-ink); }}"));
}
