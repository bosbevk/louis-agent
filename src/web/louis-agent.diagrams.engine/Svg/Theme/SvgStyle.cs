using System.Globalization;
using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Engine.Svg.Theme;

/// <summary>
/// The style inside every SVG: the theme's light colours as the picture's own variables, the dark ones when the
/// reader's screen is dark or the page it is drawn into says dark (<see cref="PageScheme"/>), and the few rules
/// every picture needs whatever look it wears: its ground, an arrowhead and a caption. A look's own rules come after these (<c>Looks/</c>).
/// </summary>
internal static class SvgStyle
{
    /// <summary>The style's text for <paramref name="theme"/>.</summary>
    public static string Of(SvgTheme theme) => string.Create(CultureInfo.InvariantCulture, $$"""
          svg {
            {{Variables(theme.Light)}}
            --mono: {{Looks.PictureFonts.Mono}};
          }
          {{PageScheme.Rules(Variables(theme.Light), Variables(theme.Dark))}}
          .ground { fill: var(--ground); }
          .arrow { fill: var(--quiet); }
          .caption {
            fill: var(--quiet); font: 600 10.5px var(--mono); letter-spacing: 1.4px; text-transform: uppercase;
          }
          {{ToneRules(theme)}}
          """);

    /// <summary>
    /// The docs' frame's look (<see cref="TitledFrame"/>), added to a framed picture's style only, so a bare
    /// picture's bytes stay as they were: the site's sans for the note's words (F160.1), and a rule for each part of
    /// the note bar.
    /// </summary>
    public const string Frame =
        $$"""

        svg {
          --sans: {{Looks.PictureFonts.Sans}};
        }
        .notebar { fill: var(--chip); stroke: var(--line); stroke-width: 1; }
        .note-icon {
          fill: none; stroke: var(--accent); stroke-width: 2;
          stroke-linecap: round; stroke-linejoin: round;
        }
        .notes { fill: var(--quiet); font: 400 12px var(--sans); }
        .note-head { fill: var(--text); font-weight: 700; }

        """;

    /// <summary>One palette as the picture's colour variables, each tone's three among them.</summary>
    internal static string Variables(SvgPalette palette) =>
        $"--ground: {palette.Ground}; --line: {palette.Line}; --text: {palette.Text}; " +
        $"--quiet: {palette.Quiet}; --chip: {palette.Chip}; --accent: {palette.Accent};" +
        string.Concat(palette.Tones.Select(tone =>
            $" --{tone.Key.Wire()}-fill: {tone.Value.Fill}; --{tone.Key.Wire()}-line: {tone.Value.Line};" +
            $" --{tone.Key.Wire()}-ink: {tone.Value.Ink};"));

    /// <summary>What a tone changes for every look: the head of an arrow that leaves a box of that tone.</summary>
    private static string ToneRules(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire()).Select(name => $".arrow.tone-{name} {{ fill: var(--{name}-line); }}"));
}
