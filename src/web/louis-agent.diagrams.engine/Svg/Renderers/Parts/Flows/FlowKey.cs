using LouisAgent.Diagrams.Engine.Flow.Keys;
using LouisAgent.Diagrams.Engine.Flow.Shapes;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;

/// <summary>
/// Draws a flow's key (F134.8): a faint rule, its heading, then each item where the layout put it, a small sample
/// of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the shapes and classes of the real thing, so the key can never show a tone or a shape
/// that looks other than the picture's own. A tone's sample is a small badge filled with it, since a box's badge
/// is what says its tone. The rule and the room under it are the other kinds' keys' (F134.12, Louis of the flow's
/// key: "do not look like the other graphs"). Each sample is a box on its row's middle line.
/// </remarks>
internal static class FlowKey
{
    /// <summary>
    /// Where the heading's words stand under the rule, how far under a row's middle an item's words do, how tall a
    /// shape's sample is, a tone's badge's corner, and how tall the rule is.
    /// </summary>
    private const double HeadingLine = 22, WordDrop = 4.5, Tall = 16, BadgeCorner = 6, Rule = 1;

    /// <summary>The key for a picture that wide, at its left, its top at a height.</summary>
    public static void Draw(SvgWriter writer, IReadOnlyList<FlowKeyItem> items, IGeometry geometry, double top,
        double width)
    {
        writer.Box(geometry.Box(0, top, width, Rule), 0, "flow-rule")
            .Text(0, top + HeadingLine, DiagramText.Format(DrawingMessage.KeyHeading), "pic-group", "start");
        foreach (var item in items)
        {
            // An item's row holds its sample, and its words stand after the sample, on the row's middle line.
            var row = geometry.Box(item.X, top + item.Y, item.Sample, FlowKeyLayout.Row);
            Sample(writer, item, row, geometry);
            writer.Text(row.Right + FlowKeyLayout.Words, row.Middle.Y + WordDrop, item.Words, "pic-means", "start");
        }
    }

    /// <summary>An item's sample, by what the item explains, in its row and on the row's middle line.</summary>
    /// <remarks>A switch on the item's kind, one arm a kind (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, FlowKeyItem item, IBox row, IGeometry geometry)
    {
        switch (item.Kind)
        {
            // A tone: a small badge filled with it, as a box's badge is.
            case FlowKeyKind.Tone:
                var badge = geometry.Box(row.Left, 0, item.Sample, item.Sample).OnLine(row.Middle.Y);
                writer.Box(badge, BadgeCorner, $"pic-fill{PictureParts.Tone(item.Tone)}");
                break;
            // A shape: the shape itself, small.
            case FlowKeyKind.Shape:
                Shape(writer, item, geometry.Box(row.Left, 0, item.Sample, Tall).OnLine(row.Middle.Y));
                break;
            // What a line's colour says: a short line with its arrow, marked as the key's so it is told from a
            // real one.
            default:
                string line = new PathWriter().From(row.Left, row.Middle.Y).Across(row.Right).Text;
                writer.Path(line, "flow-key pic-line pic-edge", true);
                break;
        }
    }

    /// <summary>A shape's sample: a pill as a rounded box, a decision or a slanted box by its outline.</summary>
    private static void Shape(SvgWriter writer, FlowKeyItem item, IBox small)
    {
        const string look = "pic-panel flow-shape pic-edge";
        if (NodeShapes.PathOf(item.Shape, small) is { } outline) writer.Path(outline, look, false);
        else writer.Box(small, small.HalfHeight, look);
    }
}
