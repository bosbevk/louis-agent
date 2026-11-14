using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Keys;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Flow.Shapes;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
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
/// It draws what <see cref="GraphLayout"/> placed by the rules a flow gets, <see cref="FlowRules.Straight"/>, and
/// decides no place itself. The look is the shared <see cref="PictureLook"/>, drawn with
/// <see cref="PictureParts"/>, and what <see cref="FlowStyle"/> adds for a flow's own shapes.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
internal static class FlowDrawing
{
    /// <summary>
    /// A plain box's corner, a badge's size, its corner and the icon's size in it, and how far a pill's and a
    /// slanted box's insides stand in from where a plain box's do.
    /// </summary>
    private const double Corner = 10, Badge = 28, BadgeCorner = 9, BadgeIcon = 16, Inside = 12;

    /// <summary>The flow a source holds, as one SVG file's text.</summary>
    public static string Of(DiagramSource source)
    {
        var placed = GraphLayout.Place(source.Flow!, bend: GraphLayout.SoftBend, rules: FlowRules.Straight);

        // The flow is the body. Its key goes across the picture's width, under a rule the accent's edge fills.
        return PictureBuilder.Titled(source.Title)
            .Paint(MarqueeLook.EdgePaint, PaintRun.Across, 0, 0.5, 1)
            .Body("flow", Flow(placed), placed.Width, placed.Height)
            .Key(width =>
            {
                var (items, height) = FlowKeyLayout.Of(placed.Nodes, width);
                return (height, (writer, top) => FlowKey.Draw(writer, items, 0, top, width));
            })
            .Caption(source.Caption)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, FlowStyle.Rules)
            .Build();
    }

    /// <summary>
    /// Everything the layout placed, from where the layout starts: the groups' panels behind, then the lines, the
    /// words on them, the boxes over the lines' ends, and last the ring where each box's line leaves.
    /// </summary>
    private static SvgWriter Flow(PlacedGraph placed)
    {
        var writer = new SvgWriter();
        foreach (var group in placed.Groups)
            PictureParts.Panel(writer, group.X, group.Y, group.W, group.H)
                .Text(group.X + 14, group.Y + 19, group.Group.Label, "pic-group", "start");

        // A line, its word and its ring are in the tone of the box the line leaves.
        var toneOf = placed.Nodes.ToDictionary(node => node.Node.Id, node => node.Node.Tone);
        foreach (var edge in placed.Edges) Line(writer, edge, toneOf.GetValueOrDefault(edge.Edge.From));
        foreach (var edge in placed.Edges.Where(edge => edge.Edge.Word is not null))
            Word(writer, edge, PictureParts.Tone(toneOf.GetValueOrDefault(edge.Edge.From)));
        foreach (var node in placed.Nodes) Box(writer, node);

        // One ring a place a line leaves from, though a box's line branches to several.
        var rings = placed.Edges.Select(edge => (At: Start(edge.Path), Tone: toneOf.GetValueOrDefault(edge.Edge.From)))
            .Distinct();
        foreach (var (at, tone) in rings)
            writer.Circle(at.X, at.Y, 4, $"pic-port pic-edge{PictureParts.Tone(tone)}");
        return writer;
    }

    /// <summary>
    /// A line with its arrowhead, and the dots that march along it. A dashed line is drawn dashed and stands still.
    /// </summary>
    private static void Line(SvgWriter writer, PlacedEdge edge, DiagramTone tone)
    {
        bool dashed = edge.Edge.Style == EdgeStyle.Dashed;
        string look = $"pic-line pic-edge{PictureParts.Tone(tone)}{(dashed ? " reads" : "")}";
        writer.Path(edge.Path, look, arrow: true, tone: tone == DiagramTone.Plain ? null : tone.Wire());
        if (!dashed) writer.Path(edge.Path, "pic-march", arrow: false);
    }

    /// <summary>An arrow's word, in a pill on its line where the layout kept the room clear.</summary>
    internal static void Word(SvgWriter writer, PlacedEdge edge, string tone)
    {
        var (x, y, w, h) = FlowText.WordBox(edge);
        writer.Box(x, y, w, h, 9, $"flow-chip pic-edge{tone}");
        var lines = FlowText.WordLines(edge.Edge.Word!);
        for (int at = 0; at < lines.Count; at++)
            writer.Text(edge.WordX, edge.WordY - FlowText.WordRise + FlowText.WordStep * at, lines[at],
                $"flow-word pic-ink{tone}");
    }

    /// <summary>
    /// A box: its panel in its shape, then what stands in it. A decision holds its words alone, in the middle
    /// under a short bar in its colour. Any other box has its icon in a solid badge in its colour, and its words
    /// after it. A box with no icon has its words alone, in the middle.
    /// </summary>
    private static void Box(SvgWriter writer, PlacedNode node)
    {
        string tone = PictureParts.Tone(node.Node.Tone);
        Panel(writer, node, tone);
        double middle = node.Y + node.H / 2, first = node.Y + FlowText.LabelY(node);
        if (node.Node.Shape == NodeShape.Decision)
        {
            writer.Box(node.X + node.W / 2 - 11, first - 24, 22, 4, 2, $"pic-fill{tone}");
            Words(writer, node, node.X + node.W / 2, "middle");
            return;
        }

        // A pill's round end and a slanted box's slant take room at the left, so what is inside stands further in.
        double left = node.X + (node.Node.Shape == NodeShape.Box ? 0 : Inside);
        if (node.Node.Icon is not { } icon)
        {
            Words(writer, node, node.X + node.W / 2, "middle");
            return;
        }

        writer.Box(left + 16, middle - Badge / 2, Badge, Badge, BadgeCorner, $"pic-fill{tone}")
            .Icon(left + 16 + (Badge - BadgeIcon) / 2, middle - BadgeIcon / 2, BadgeIcon, icon, "flow-icon");
        Words(writer, node, FlowText.LabelLeft(node) + (left - node.X), "start");
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
        if (NodeShapes.PathOf(node) is { } outline)
        {
            writer.Path(outline, look, arrow: false);
            return;
        }

        writer.Box(node.X, node.Y, node.W, node.H, plain ? Corner : node.H / 2, look);
    }

    /// <summary>A box's words: its label's lines, and its note's under them.</summary>
    private static void Words(SvgWriter writer, PlacedNode node, double x, string anchor)
    {
        var label = FlowText.LabelLines(node.Node);
        for (int at = 0; at < label.Count; at++)
            writer.Text(x, node.Y + FlowText.LabelY(node) + FlowText.LabelStep * at, label[at], "flow-name", anchor);
        var note = node.Node.Note is { } words ? GraphLayout.NoteLines(words) : [];
        for (int at = 0; at < note.Count; at++)
            writer.Text(x, node.Y + FlowText.NoteY(node) + FlowText.NoteStep * at, note[at], "pic-note", anchor);
    }

    /// <summary>Where a path starts: the two numbers after its first letter.</summary>
    private static (double X, double Y) Start(string path)
    {
        double[] start =
        [
            .. path.Split(' ').Skip(1).Take(2).Select(part => double.Parse(part, CultureInfo.InvariantCulture))
        ];
        return (start[0], start[1]);
    }
}
