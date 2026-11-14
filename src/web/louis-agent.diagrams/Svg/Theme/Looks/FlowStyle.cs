namespace LouisAgent.Diagrams.Svg.Theme.Looks;

/// <summary>
/// What a flow's look adds to the shared one (F134.8): a box's name and the icon in its badge, the thin outline of
/// a shape that means something, the pill an arrow's word stands in, and the rule over its key.
/// </summary>
/// <remarks>
/// Everything a component diagram wears too, the panels, the lines and their marching dots, is
/// <see cref="PictureLook"/>'s. Its rules come first in the picture's style, and these after them. A decision,
/// data and a start or an end have an outline, grey until the shape's tone colours it, so each reads as its shape
/// on a panel that is otherwise a plain box's. They glowed before, and no other kind glows (F134.12, Louis: "make
/// the theme of the flow and component diagrams resemble the others"). A group's frame is a panel, so it has no
/// rule here. The same rules serve every theme, since their colours are the theme's own variables.
/// </remarks>
internal static class FlowStyle
{
    /// <summary>The style.</summary>
    public const string Rules =
        $$"""

          .flow-name { fill: var(--text); font: 700 14px var(--pic-sans); letter-spacing: -.1px; }
          .flow-icon {
            fill: none; stroke: var(--ground); stroke-width: 2; stroke-linecap: round; stroke-linejoin: round;
          }
          .flow-shape { stroke: var(--quiet); stroke-width: 1.25; stroke-opacity: .8; stroke-linejoin: round; }
          .flow-chip { fill: var(--ground); stroke-width: 1; stroke-opacity: .6; }
          .flow-word { font: 500 10px var(--mono); }
          .flow-rule { fill: url(#{{MarqueeLook.EdgePaint}}); opacity: .7; }
          """;
}
