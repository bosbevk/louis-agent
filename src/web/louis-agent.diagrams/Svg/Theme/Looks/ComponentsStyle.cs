using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Svg.Theme.Looks;

/// <summary>
/// What a component diagram's look adds to the shared one (F134.7): the pins on a box's edge, the pill of the one
/// thing a box holds with its glow and its pulse, the dashed box of what stands in for another, and the faces of a
/// chip and a neighbour's code.
/// </summary>
/// <remarks>
/// Everything a flow wears too, the ground, the panels, the lines and their marching dots, is
/// <see cref="PictureLook"/>'s. Its rules come first in the picture's style, and these after them.
/// </remarks>
internal static class ComponentsStyle
{
    /// <summary>The style, for a theme's tones.</summary>
    public static string Of(SvgTheme theme) =>
        $$"""

          .cmp-code { font: 500 11px var(--mono); }
          .cmp-ghost { fill: none; stroke-width: 1; stroke-dasharray: 4 4; stroke-opacity: .7; }
          .cmp-code-name { fill: var(--text); font: 600 12.5px var(--mono); }
          .cmp-chip-text, .cmp-pin-text { font: 500 10px var(--mono); }
          .cmp-pin { stroke-width: 1.25; }
          .cmp-pin.up { fill: var(--ground); }
          .cmp-pill { stroke-width: 1.5; filter: drop-shadow(0 0 8px var(--quiet)); }
          {{Glows(theme)}}
          .cmp-pill-text { fill: var(--text); font: 600 12.5px var(--pic-sans); }
          .cmp-pulse { animation: cmp-pulse 2.4s ease-in-out infinite; }
          @keyframes cmp-pulse { 50% { opacity: .35; } }
          @media (prefers-reduced-motion: reduce) {
            .cmp-pulse { animation: none; }
          }
          """;

    /// <summary>A rule a tone: a pill glows in the colour of the box that holds it.</summary>
    private static string Glows(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire())
        .Select(name => $".cmp-pill.tone-{name} {{ filter: drop-shadow(0 0 8px var(--{name}-line)); }}"));
}
