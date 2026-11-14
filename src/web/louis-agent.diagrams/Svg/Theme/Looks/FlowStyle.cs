using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Svg.Theme.Looks;

/// <summary>
/// What a flow's look adds to the shared one (F134.8): the frame of a group, a box's name and the icon in its
/// badge, the full outline and soft glow of a shape that means something, and the pill an arrow's word stands in.
/// </summary>
/// <remarks>
/// Everything a component diagram wears too, the ground, the panels, the lines and their marching dots, is
/// <see cref="PictureLook"/>'s. Its rules come first in the picture's style, and these after them. A decision,
/// data and a start or an end glow in their tone so the eye finds them first (Louis: the shapes must pop as a
/// component's panel does).
/// </remarks>
internal static class FlowStyle
{
    /// <summary>The style, for a theme's tones.</summary>
    public static string Of(SvgTheme theme) =>
        $$"""

          .flow-group {
            fill: var(--pic-1); stroke: var(--line); stroke-width: 1; filter: drop-shadow(0 8px 9px var(--pic-lift));
          }
          .flow-name { fill: var(--text); font: 600 14px var(--pic-sans); }
          .flow-icon {
            fill: none; stroke: var(--ground); stroke-width: 2; stroke-linecap: round; stroke-linejoin: round;
          }
          .flow-shape {
            stroke-width: 1.5; stroke-opacity: 1; stroke-linejoin: round;
            filter: drop-shadow(0 0 7px color-mix(in srgb, var(--flow-glow, var(--quiet)) 55%, transparent))
              drop-shadow(0 8px 9px var(--pic-lift));
          }
          {{Glows(theme)}}
          .flow-chip { fill: var(--ground); stroke-width: 1; stroke-opacity: .6; }
          .flow-word { font: 500 10px var(--mono); }
          """;

    /// <summary>A rule a tone: a shape glows in its own colour.</summary>
    private static string Glows(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire())
        .Select(name => $".flow-shape.tone-{name} {{ --flow-glow: var(--{name}-line); }}"));
}
