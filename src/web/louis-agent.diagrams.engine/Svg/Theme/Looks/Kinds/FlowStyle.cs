namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a flow's look adds to the shared one (F134.8): the thin outline of a shape that means something.
/// </summary>
/// <remarks>
/// Everything another kind wears too is a shared look's: the panels, the lines and their marching dots, a box's
/// name, the icon in its badge and the pill an arrow's word stands in are <see cref="PictureLook"/>'s, and the
/// rule over a key is <see cref="MarqueeLook"/>'s. Their rules come first in the picture's style, and this after
/// them. A decision,
/// data and a start or an end have an outline, grey until the shape's tone colours it, so each reads as its shape
/// on a panel that is otherwise a plain box's. They glowed before, and no other kind glows (F134.12, Louis: "make
/// the theme of the flow and component diagrams resemble the others"). A group's frame is a panel, so it has no
/// rule here. The same rules serve every theme, since their colours are the theme's own variables.
/// </remarks>
internal static class FlowStyle
{
    /// <summary>The style.</summary>
    public const string Rules =
        """

        .flow-shape { stroke: var(--quiet); stroke-width: 1.25; stroke-opacity: .8; stroke-linejoin: round; }
        """;
}
