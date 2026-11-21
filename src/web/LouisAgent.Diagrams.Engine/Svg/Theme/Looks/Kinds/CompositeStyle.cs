namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a composite structure diagram's look adds to the ones it wears (F134.13): the plain line between two parts
/// that work together, and the face of what is asked or needed, written outside a pin.
/// </summary>
/// <remarks>
/// Almost nothing is its own, on purpose. The panels and the lines that run are <see cref="PictureLook"/>'s, the
/// title, the type and the light <see cref="MarqueeLook"/>'s, a line's word and the key's rule
/// <see cref="FlowStyle"/>'s, and a pin and a chip <see cref="NestedStyle"/>'s. So a composite can't drift
/// from the kinds it borrows from. A plain line has no arrowhead and no dots: it says two parts work together, not
/// that something moves along it.
/// </remarks>
internal static class CompositeStyle
{
    /// <summary>The style. It is the same for every theme, since its colours are the theme's own variables.</summary>
    public const string Rules =
        """

        .cps-wire {
          fill: none; stroke: var(--quiet); stroke-width: 1.5; stroke-linecap: round; stroke-linejoin: round;
        }
        .cps-words { font: 500 11px var(--mono); }
        """;
}
