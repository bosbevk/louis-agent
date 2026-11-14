using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Keys;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Flow.Shapes;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Types;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;

/// <summary>
/// Draws a flow in the new look (F134.8, the others' look since F134.12): its title as one small line over a soft
/// light, boxes as panels in their shape, each arrow at its own point with dots that march to its head, a ring
/// where a box's line leaves, and the key that says how to read it.
/// </summary>
/// <remarks>
/// It draws what <see cref="GraphLayout"/> placed, and decides no place itself. The look is the shared
/// <see cref="PictureLook"/>, drawn with <see cref="PictureParts"/>, and what <see cref="FlowStyle"/> adds for a
/// flow's own shapes. What stands inside a box, its badge and its bar, is a box of its own, asked of the geometry
/// the picture is drawn with.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
internal static class FlowDrawing
{
    /// <summary>
    /// A plain box's corner, a badge's size, its corner and the icon's size in it, and how far a pill's and a
    /// slanted box's insides stand in from where a plain box's do.
    /// </summary>
    private const double Corner = 10, Badge = 28, BadgeCorner = 9, BadgeIcon = 16, Inside = 12;

    /// <summary>
    /// How far in from a box's left its badge stands, and from a group's frame its label: across, and down.
    /// </summary>
    private const double BadgeIn = 16, GroupLabelIn = 14, GroupLabelLine = 19;

    /// <summary>
    /// The short bar over a decision's words: how wide and how tall it is, its corner, and how far above the
    /// label's first line it stands.
    /// </summary>
    private const double BarWidth = 22, BarTall = 4, BarCorner = 2, BarRise = 24;

    /// <summary>The corner of a word's pill, and the ring where a box's line leaves.</summary>
    private const double ChipCorner = 9, Ring = 4;

    /// <summary>The flow a source holds, as one SVG file's text, drawn with a geometry.</summary>
    public static string Of(DiagramSource source, IGeometry geometry)
    {
        var placed = GraphLayout.Place(source.Flow!, bend: GraphLayout.SoftBend,
            geometry: geometry);

        // The flow is the body. Its key goes across the picture's width, under a rule the accent's edge fills.
        return PictureBuilder.Titled(source.Title)
            .Type(TypeNames.Of(source))
            .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, 0.5, 1)
            .Body("flow", Flow(placed, geometry), placed.Width, placed.Height)
            .Key(width =>
            {
                var (items, height) = FlowKeyLayout.Of(placed.Nodes, width);
                return (height, (writer, top) => FlowKey.Draw(writer, items, geometry, top, width));
            })
            .Caption(source.Caption)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, FlowStyle.Rules)
            .Build();
    }

    /// <summary>
    /// Everything the layout placed, from where the layout starts: the groups' panels behind, then the lines, the
    /// words on them, the boxes over the lines' ends, and last the ring where each box's line leaves.
    /// </summary>
    private static SvgWriter Flow(PlacedGraph placed, IGeometry geometry)
    {
        var writer = new SvgWriter();
        foreach (var group in placed.Groups)
            PictureParts.Panel(writer, group.Box).Text(group.Box.Left + GroupLabelIn, group.Box.Top + GroupLabelLine,
                group.Group.Label, "pic-group", "start");

        // A line, its word and its ring are in the tone of the box the line leaves.
        var toneOf = placed.Nodes.ToDictionary(node => node.Node.Id, node => node.Node.Tone);
        foreach (var edge in placed.Edges) Line(writer, edge, toneOf.GetValueOrDefault(edge.Edge.From));
        foreach (var edge in placed.Edges.Where(edge => edge.Edge.Word is not null))
            Word(writer, edge, PictureParts.Tone(toneOf.GetValueOrDefault(edge.Edge.From)), geometry);
        foreach (var node in placed.Nodes) Box(writer, node, geometry);

        // One ring a place a line leaves from, though a box's line branches to several.
        var rings = placed.Edges.Select(edge => (At: edge.Way.Start, Tone: toneOf.GetValueOrDefault(edge.Edge.From)))
            .Distinct();
        foreach (var (at, tone) in rings)
            writer.Circle(at.X, at.Y, Ring, $"pic-port pic-edge{PictureParts.Tone(tone)}");
        return writer;
    }

    /// <summary>
    /// A line with its arrowhead, and the dots that march along it. A dashed line is drawn dashed and stands still.
    /// </summary>
    private static void Line(SvgWriter writer, PlacedEdge edge, DiagramTone tone)
    {
        bool dashed = edge.Edge.Style == EdgeStyle.Dashed;
        string look = $"pic-line pic-edge{PictureParts.Tone(tone)}{(dashed ? " reads" : "")}";
        writer.Path(edge.Way.Text, look, arrow: true, tone: tone == DiagramTone.Plain ? null : tone.Wire());
        if (!dashed) writer.Path(edge.Way.Text, "pic-march", arrow: false);
    }

    /// <summary>An arrow's word, in a pill on its line where the layout kept the room clear.</summary>
    internal static void Word(SvgWriter writer, PlacedEdge edge, string tone, IGeometry geometry)
    {
        writer.Box(FlowText.WordBox(edge, geometry), ChipCorner, $"flow-chip pic-edge{tone}");
        var lines = FlowText.WordLines(edge.Edge.Word!);
        for (int at = 0; at < lines.Count; at++)
            writer.Text(edge.Word.X, edge.Word.Y - FlowText.WordRise + FlowText.WordStep * at, lines[at],
                $"flow-word pic-ink{tone}");
    }

    /// <summary>
    /// A box: its panel in its shape, then what stands in it. A decision holds its words alone, in the middle
    /// under a short bar in its colour. Any other box has its icon in a solid badge in its colour, and its words
    /// after it. A box with no icon has its words alone, in the middle.
    /// </summary>
    private static void Box(SvgWriter writer, PlacedNode node, IGeometry geometry)
    {
        string tone = PictureParts.Tone(node.Node.Tone);
        var box = node.Box;
        Panel(writer, node, tone);
        if (node.Node.Shape == NodeShape.Decision)
        {
            double first = box.Top + FlowText.LabelY(node);
            var bar = geometry.Box(box.Middle.X - BarWidth / 2, first - BarRise, BarWidth, BarTall);
            writer.Box(bar, BarCorner, $"pic-fill{tone}");
            Words(writer, node, box.Middle.X, "middle");
            return;
        }

        // A pill's round end and a slanted box's slant take room at the left, so what is inside stands further in.
        double left = box.Left + (node.Node.Shape == NodeShape.Box ? 0 : Inside);
        if (node.Node.Icon is not { } icon)
        {
            Words(writer, node, box.Middle.X, "middle");
            return;
        }

        // The badge stands on the box's middle line, and the icon in the badge's middle.
        var badge = geometry.Box(left + BadgeIn, 0, Badge, Badge).OnLine(box.Middle.Y);
        var sign = geometry.Box(badge.Left + (Badge - BadgeIcon) / 2, 0, BadgeIcon, BadgeIcon).OnLine(box.Middle.Y);
        writer.Box(badge, BadgeCorner, $"pic-fill{tone}").Icon(sign.Left, sign.Top, BadgeIcon, icon, "flow-icon");
        Words(writer, node, FlowText.LabelLeft(node) + (left - box.Left), "start");
    }

    /// <summary>
    /// A box's panel: a rounded box for a plain box and, with a round end, for a pill, and the outline's path for
    /// a decision and a slanted box. A plain box is a panel and no more. A shape that means something is marked
    /// and wears its tone, so the look outlines it in its colour.
    /// </summary>
    private static void Panel(SvgWriter writer, PlacedNode node, string tone)
    {
        bool plain = node.Node.Shape == NodeShape.Box;
        string look = plain ? "pic-panel" : $"pic-panel flow-shape pic-edge{tone}";
        if (NodeShapes.PathOf(node.Node.Shape, node.Box) is { } outline)
        {
            writer.Path(outline, look, arrow: false);
            return;
        }

        writer.Box(node.Box, plain ? Corner : node.Box.HalfHeight, look);
    }

    /// <summary>A box's words: its label's lines, and its note's under them.</summary>
    private static void Words(SvgWriter writer, PlacedNode node, double x, string anchor)
    {
        double top = node.Box.Top;
        var label = FlowText.LabelLines(node.Node);
        for (int at = 0; at < label.Count; at++)
            writer.Text(x, top + FlowText.LabelY(node) + FlowText.LabelStep * at, label[at], "flow-name", anchor);
        var note = node.Node.Note is { } words ? GraphLayout.NoteLines(words) : [];
        for (int at = 0; at < note.Count; at++)
            writer.Text(x, top + FlowText.NoteY(node) + FlowText.NoteStep * at, note[at], "pic-note", anchor);
    }
}
