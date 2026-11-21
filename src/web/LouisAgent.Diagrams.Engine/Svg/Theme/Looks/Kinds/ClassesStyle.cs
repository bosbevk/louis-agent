using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a class chart's look adds to the shared ones (F134.11, the design Louis approved): a type's badge in its
/// tone with a letter for its kind, its members, a line for each kind of link with its ends, the colour of a
/// word's pill by its link, and the key's own parts. A type's name and kind, the pill itself and a key's words
/// are <see cref="PictureLook"/>'s.
/// </summary>
/// <remarks>
/// A link's kind is in its line's colour and not the box it leaves, which is what a flow's line says: the
/// structure, holds, is a and implements, is violet, a creates is blue, and a use is grey and thin, so a chart's
/// skeleton stands out from what merely calls what. The tones are on the badges only. The small title, the light
/// and the glass are <see cref="MarqueeLook"/>'s, and the running dots <see cref="PictureLook"/>'s. The ends are
/// the writer's (<see cref="Ends.LineEnd"/>), and this look colours them.
/// </remarks>
internal static class ClassesStyle
{
    /// <summary>The style, for a theme's tones.</summary>
    public static string Of(SvgTheme theme) =>
        $$"""

          .cls-cover { fill: var(--ground); }
          .cls-group { fill: none; stroke: var(--mrq-hair); stroke-width: 1; }
          .cls-badge { fill: var(--quiet); }
          {{Toned(theme, ".cls-badge", "fill")}}
          .cls-mark { fill: var(--ground); font: 700 13px var(--mono); }
          .cls-divide { fill: none; stroke: var(--mrq-hair); stroke-width: 1; }
          .cls-member { fill: var(--text); fill-opacity: .82; font: 11px var(--mono); }
          .cls-line { fill: none; stroke-linecap: round; }
          .cls-line.holds { stroke: var(--violet-line); stroke-width: 2; }
          .cls-line.inherits { stroke: var(--violet-line); stroke-width: 2; }
          .cls-line.implements { stroke: var(--violet-line); stroke-width: 1.8; stroke-dasharray: 6 5; }
          .cls-line.creates { stroke: var(--blue-line); stroke-width: 1.6; stroke-dasharray: 2 5; }
          .cls-line.uses {
            stroke: var(--quiet); stroke-opacity: .75; stroke-width: 1.3; stroke-dasharray: 3 6;
          }
          .cls-chip.holds { stroke: var(--violet-line); }
          .cls-chip.inherits { stroke: var(--violet-line); }
          .cls-chip.implements { stroke: var(--violet-line); }
          .cls-chip.creates { stroke: var(--blue-line); }
          .cls-chip.uses { stroke: var(--quiet); }
          .cls-word { fill: var(--text); }
          .cls-word.quiet { fill: var(--quiet); }
          .end-triangle { fill: var(--ground); stroke: var(--quiet); stroke-width: 1.6; stroke-linejoin: round; }
          {{Toned(theme, ".end-triangle", "stroke")}}
          .end-diamond { fill: var(--quiet); }
          {{Toned(theme, ".end-diamond", "fill")}}
          .end-open {
            fill: none; stroke: var(--quiet); stroke-width: 1.4; stroke-linecap: round; stroke-linejoin: round;
          }
          {{Toned(theme, ".end-open", "stroke")}}
          .cls-key-mark { fill: none; stroke: var(--quiet); stroke-width: 1; stroke-opacity: .6; }
          .cls-key-letter { fill: var(--text); font: 700 11px var(--mono); }
          """;

    /// <summary>A rule a tone: a part takes its tone's colour, as its fill or as its stroke.</summary>
    private static string Toned(SvgTheme theme, string part, string paint) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire())
        .Select(name => $"{part}.tone-{name} {{ {paint}: var(--{name}-line); }}"));
}
