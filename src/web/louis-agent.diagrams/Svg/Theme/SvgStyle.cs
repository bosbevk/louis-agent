using System.Globalization;

namespace LouisAgent.Diagrams.Svg.Theme;

/// <summary>
/// The style inside every SVG: the theme's light colours as the picture's own variables, the dark ones when the
/// reader's screen is dark, and one rule per kind of shape that reads them. The site's look: the code font, a box
/// like the site's code listing, quiet arrows, words on a halo of the ground.
/// </summary>
internal static class SvgStyle
{
    /// <summary>The style's text for <paramref name="theme"/>.</summary>
    public static string Of(SvgTheme theme) => string.Create(CultureInfo.InvariantCulture, $$"""
          svg {
            {{Variables(theme.Light)}}
            --mono: "Geist Mono", ui-monospace, Menlo, Consolas, monospace;
          }
          @media (prefers-color-scheme: dark) {
            svg { {{Variables(theme.Dark)}} }
          }
          .ground { fill: var(--ground); }
          .box { fill: var(--panel); stroke: var(--line); stroke-width: 1; }
          .label { fill: var(--text); font: 600 12px var(--mono); }
          .note { fill: var(--quiet); font: 10px var(--mono); }
          .edge { fill: none; stroke: var(--quiet); stroke-width: 1.5; }
          .arrow { fill: var(--quiet); }
          .dashed { stroke-dasharray: 5 4; }
          .group { fill: var(--chip); stroke: var(--line); stroke-width: 1; }
          .group-label {
            fill: var(--quiet); font: 600 10px var(--mono); letter-spacing: 1px; text-transform: uppercase;
          }
          .word {
            fill: var(--text); font: 600 10px var(--mono); letter-spacing: 1px; text-transform: uppercase;
            paint-order: stroke; stroke: var(--ground); stroke-width: 5px; stroke-linejoin: round;
          }
          .caption {
            fill: var(--quiet); font: 600 10.5px var(--mono); letter-spacing: 1.4px; text-transform: uppercase;
          }
          .icon { fill: none; stroke: var(--text); stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }
          .frame { fill: none; stroke: var(--line); stroke-width: 1; }
          .card { fill: var(--panel); stroke: var(--line); stroke-width: 1; }
          .highlight { fill: var(--tint); stroke: var(--accent); }
          .title { fill: var(--text); font: 600 13px var(--mono); }
          .chip { fill: var(--chip); stroke: var(--line); stroke-width: 1; }
          .code { fill: var(--text); font: 11px var(--mono); }
          .text { fill: var(--quiet); font: 11px var(--mono); }
          """);

    /// <summary>One palette as the picture's colour variables.</summary>
    private static string Variables(SvgPalette palette) =>
        $"--ground: {palette.Ground}; --panel: {palette.Panel}; --line: {palette.Line}; --text: {palette.Text}; " +
        $"--bright: {palette.Bright}; --quiet: {palette.Quiet}; --chip: {palette.Chip}; " +
        $"--accent: {palette.Accent}; --tint: {palette.Tint};";
}
