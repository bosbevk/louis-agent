using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Theme;

namespace LouisAgent.Diagrams.Svg;

/// <summary>
/// Draws a flow: the site's boxes and arrows, placed by <see cref="GraphLayout"/> by ADR 0103's rules, each box's
/// words where the site's <c>GraphView</c> puts them, and the caption under the drawing.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
internal sealed class FlowRenderer : IDiagramRenderer
{
    /// <summary>The room under the drawing a caption takes.</summary>
    private const double CaptionRoom = 34;

    public DiagramKind Kind => DiagramKind.Flow;

    public string Render(DiagramSource source)
    {
        var placed = GraphLayout.Place(source.Flow!);
        var writer = new SvgWriter();

        // The groups' frames first, behind everything, each labelled at its top left.
        foreach (var group in placed.Groups)
            writer.Box(group.X, group.Y, group.W, group.H, 12, "group")
                .Text(group.X + 12, group.Y + 18, group.Group.Label, "group-label", "start");

        // The arrows next, so the boxes stand over their ends; a word sits just above its arrow's flat piece.
        foreach (var edge in placed.Edges)
        {
            writer.Path(edge.Path, edge.Edge.Style == EdgeStyle.Dashed ? "edge dashed" : "edge", arrow: true);
            var lines = edge.Edge.Word is { } word ? FlowText.WordLines(word) : [];
            for (int at = 0; at < lines.Count; at++)
                writer.Text(edge.WordX, edge.WordY - 7 + FlowText.WordStep * at, lines[at], "word");
        }

        foreach (var node in placed.Nodes)
        {
            double middle = node.X + node.W / 2;
            writer.Box(node.X, node.Y, node.W, node.H, 10, "box");
            var label = FlowText.LabelLines(node.Node);
            for (int at = 0; at < label.Count; at++)
                writer.Text(middle, node.Y + FlowText.LabelY(node) + FlowText.LabelStep * at, label[at], "label");
            var lines = node.Node.Note is { } note ? GraphLayout.NoteLines(note) : [];
            for (int at = 0; at < lines.Count; at++)
                writer.Text(middle, node.Y + FlowText.NoteY(node) + FlowText.NoteStep * at, lines[at], "note");
        }

        if (source.Caption is not { } caption)
            return writer.Write(SvgTheme.Site, source.Title, placed.Width, placed.Height);
        writer.Text(0, placed.Height + CaptionRoom - 8, caption, "caption", "start");
        return writer.Write(SvgTheme.Site, source.Title, placed.Width, placed.Height + CaptionRoom);
    }
}
