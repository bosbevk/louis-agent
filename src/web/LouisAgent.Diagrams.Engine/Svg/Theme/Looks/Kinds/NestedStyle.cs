namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a nested picture's look adds to the shared one (F134.7): the pins on a box's edge, the pill of the one
/// thing a box holds with its pulse, the dashed box of what stands in for another, and the faces of a chip and a
/// neighbour's code.
/// </summary>
/// <remarks>
/// Everything a flow wears too, the panels, the lines and their marching dots, is <see cref="PictureLook"/>'s. Its
/// rules come first in the picture's style, and these after them. The pill glowed in its box's colour until
/// F134.12, and no other kind glows: its tint, its outline and its pulsing dot say it is the thing held. The same
/// rules serve every theme, since their colours are the theme's own variables.
/// </remarks>
internal static class NestedStyle
{
    /// <summary>The style.</summary>
    public const string Rules =
        """

        .cmp-code { font: 500 11px var(--mono); }
        .cmp-ghost { fill: none; stroke-width: 1; stroke-dasharray: 4 4; stroke-opacity: .7; }
        .cmp-code-name { fill: var(--text); font: 600 12.5px var(--mono); }
        .cmp-chip-text, .cmp-pin-text { font: 500 10px var(--mono); }
        .cmp-pin { stroke-width: 1.25; }
        .cmp-pin.up { fill: var(--ground); }
        .cmp-pill { stroke-width: 1.5; }
        .cmp-pill-text { fill: var(--text); font: 600 12.5px var(--pic-sans); }
        .cmp-pulse { animation: cmp-pulse 2.4s ease-in-out infinite; }
        @keyframes cmp-pulse { 50% { opacity: .35; } }
        @media (prefers-reduced-motion: reduce) {
          .cmp-pulse { animation: none; }
        }
        """;
}
