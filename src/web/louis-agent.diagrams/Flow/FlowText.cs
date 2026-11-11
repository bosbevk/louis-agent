using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow;

/// <summary>
/// Where a box's words sit inside it: the site's <c>GraphView</c> and the docs' SVG both draw them here, so a box
/// reads the same on a page and in a doc.
/// </summary>
public static class FlowText
{
    /// <summary>Where the note's first line sits, from the box's top, each line one step lower.</summary>
    public const double NoteTop = 44, NoteStep = 12;

    /// <summary>How far a label's next line stands under the one before (CL60.2).</summary>
    public const double LabelStep = 16;

    /// <summary>A label's lines: a <c>\n</c> breaks it, as a docs flowchart's <c>&lt;br/&gt;</c> did.</summary>
    public static IReadOnlyList<string> LabelLines(GraphNode node) => node.Label.Split('\n');

    /// <summary>
    /// Where the label's first line sits: its lines centred in a plain box; in a noted one, from the top, above the
    /// note's lines.
    /// </summary>
    public static double LabelY(PlacedNode node) =>
        node.Node.Note is null ? node.H / 2 + 5 - (LabelLines(node.Node).Count - 1) * LabelStep / 2 : 28;

    /// <summary>Where the note's first line sits, under however many lines the label has.</summary>
    public static double NoteY(PlacedNode node) => NoteTop + (LabelLines(node.Node).Count - 1) * LabelStep;
}
