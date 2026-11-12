using LouisAgent.Diagrams.Flow.Placed;

namespace LouisAgent.Diagrams.Flow;

/// <summary>
/// Where a box's words sit inside it: the site's <c>GraphView</c> and the docs' SVG both draw them here, so a box
/// reads the same on a page and in a doc.
/// </summary>
public static class FlowText
{
    /// <summary>Where the note's first line sits, from the box's top, each line one step lower.</summary>
    public const double NoteTop = 44, NoteStep = 12;

    /// <summary>
    /// The label sits in the middle of a plain box; in a noted one it stands above the note's lines, which start under
    /// it.
    /// </summary>
    public static double LabelY(PlacedNode node) => node.Node.Note is null ? node.H / 2 + 5 : 28;
}
