namespace LouisAgent.Web.Components.Widget;

/// <summary>
/// How the chat widget stands on a page (F134.3): in a corner, a button that opens a panel, or as a bar at the foot
/// of the page with the box always there to type in.
/// </summary>
/// <remarks>
/// One engine with a layout, as F134's design says (Louis: "we should have like chatwidget layout type"): the widget's
/// runner, its door and its tool calls are the same in both, and only what it draws differs. A site picks one where it
/// places the widget.
/// </remarks>
public enum WidgetLayout
{
    /// <summary>A button in a corner that opens a panel, as every site has had.</summary>
    Corner,

    /// <summary>A bar at the foot of the page, with the box always there.</summary>
    Bar
}
