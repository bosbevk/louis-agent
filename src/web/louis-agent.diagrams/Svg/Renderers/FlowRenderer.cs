using LouisAgent.Choices;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Renderers;

/// <summary>
/// Draws a flow: the site's boxes and arrows, placed by <see cref="GraphLayout"/> by ADR 0103's rules, each box's
/// words where the site's <c>GraphView</c> puts them, and the caption under the drawing.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
internal sealed class FlowRenderer : IDiagramRenderer
{
    /// <summary>The room under the drawing a caption takes.</summary>
    private const double CaptionRoom = 34;

    /// <summary>How big an icon is drawn inside its badge.</summary>
    private const double BadgeIcon = 16;

    public DiagramKind Kind => DiagramKind.Flow;

    public string Render(DiagramSource source)
    {
        var placed = GraphLayout.Place(source.Flow!, bend: GraphLayout.SoftBend);
        var writer = new SvgWriter();

        // The groups' frames first, behind everything, each labelled at its top left.
        foreach (var group in placed.Groups)
            writer.Box(group.X, group.Y, group.W, group.H, 12, "group")
                .Text(group.X + 12, group.Y + 18, group.Group.Label, "group-label", "start");

        // The arrows next, each in the tone of the box it leaves, so the boxes stand over their ends.
        var toneOf = placed.Nodes.ToDictionary(node => node.Node.Id, node => node.Node.Tone);
        foreach (var edge in placed.Edges)
        {
            var tone = toneOf.GetValueOrDefault(edge.Edge.From);
            string dashed = edge.Edge.Style == EdgeStyle.Dashed ? " dashed" : "";
            writer.Path(edge.Path, "edge" + dashed + Class(tone), arrow: true,
                tone: tone == DiagramTone.Plain ? null : tone.Wire());
        }

        // Every word after every arrow, so no line runs over a chip; each chip where the layout kept it clear.
        foreach (var edge in placed.Edges.Where(edge => edge.Edge.Word is not null))
        {
            string tone = Class(toneOf.GetValueOrDefault(edge.Edge.From));
            var (x, y, w, h) = FlowText.WordBox(edge);
            writer.Box(x, y, w, h, 4, "word-chip" + tone);
            var lines = FlowText.WordLines(edge.Edge.Word!);
            for (int at = 0; at < lines.Count; at++)
                writer.Text(edge.WordX, edge.WordY - FlowText.WordRise + FlowText.WordStep * at, lines[at],
                    "word" + tone);
        }

        foreach (var node in placed.Nodes) Draw(writer, node);

        if (source.Caption is not { } caption)
            return writer.Write(SvgTheme.Site, source.Title, placed.Width, placed.Height);
        writer.Text(0, placed.Height + CaptionRoom - 8, caption, "caption", "start");
        return writer.Write(SvgTheme.Site, source.Title, placed.Width, placed.Height + CaptionRoom);
    }

    /// <summary>
    /// One box in its tone: with an icon, the icon in a round badge at its left and the words from the left after it
    /// (Louis's pick, CL60.2); without one, the words centred, as the site's boxes are.
    /// </summary>
    private static void Draw(SvgWriter writer, PlacedNode node)
    {
        string tone = Class(node.Node.Tone);
        writer.Box(node.X, node.Y, node.W, node.H, 10, "box" + tone);

        (double x, string anchor) = (node.X + node.W / 2, "middle");
        if (node.Node.Icon is { } icon)
        {
            // The badge stands in the middle of the box's height; the icon in it is drawn a little smaller.
            double centreX = node.X + FlowText.BadgeLeft + FlowText.BadgeSize / 2;
            double centreY = node.Y + FlowText.HeadHeight(node) / 2;
            writer.Circle(centreX, centreY, FlowText.BadgeSize / 2, "badge" + tone)
                .Icon(centreX - BadgeIcon / 2, centreY - BadgeIcon / 2, BadgeIcon, icon, "badge-icon");
            (x, anchor) = (FlowText.LabelLeft(node), "start");
        }

        var label = FlowText.LabelLines(node.Node);
        for (int at = 0; at < label.Count; at++)
            writer.Text(x, node.Y + FlowText.LabelY(node) + FlowText.LabelStep * at, label[at], "label" + tone,
                anchor);
        var lines = node.Node.Note is { } note ? GraphLayout.NoteLines(note) : [];
        for (int at = 0; at < lines.Count; at++)
            writer.Text(x, node.Y + FlowText.NoteY(node) + FlowText.NoteStep * at, lines[at], "note", anchor);

        // A class chart's members (CL60.4): a divider under the head, then each member from the left.
        if (node.Node.Members is not { Count: > 0 }) return;
        var members = FlowText.MemberLines(node.Node);
        double divider = node.Y + FlowText.HeadHeight(node);
        string y = SvgWriter.N(divider);
        writer.Path($"M {SvgWriter.N(node.X)} {y} L {SvgWriter.N(node.X + node.W)} {y}", "frame", arrow: false);
        for (int at = 0; at < members.Count; at++)
            writer.Text(node.X + GraphLayout.LabelPad, divider + FlowText.MemberTop + 10 + FlowText.MemberStep * at,
                members[at], "word" + tone, "start");
    }

    /// <summary>
    /// The class a shape adds for its tone: none for plain, so a plain shape draws as it always did. The sequences
    /// wear the same (CL60.3).
    /// </summary>
    internal static string Class(DiagramTone tone) => tone == DiagramTone.Plain ? "" : $" tone-{tone.Wire()}";
}
