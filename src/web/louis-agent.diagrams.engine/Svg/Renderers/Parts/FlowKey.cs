using LouisAgent.Diagrams.Engine.Flow.Keys;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Flow.Shapes;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;

/// <summary>
/// Draws a flow's key (F134.8): a faint rule, its heading, then each item where the layout put it, a small sample
/// of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the shapes and classes of the real thing, so the key can never show a tone or a shape
/// that looks other than the picture's own. A tone's sample is a small badge filled with it, since a box's badge
/// is what says its tone. The rule and the room under it are the other kinds' keys' (F134.12, Louis of the flow's
/// key: "do not look like the other graphs").
/// </remarks>
internal static class FlowKey
{
    /// <summary>
    /// Where the heading's words stand under the rule, how far under a row's middle an item's words do, how tall a
    /// shape's sample is, and a tone's badge's corner.
    /// </summary>
    private const double HeadingLine = 22, WordDrop = 4.5, Tall = 16, BadgeCorner = 6;

    /// <summary>The key for a picture that wide, its top-left corner at a point.</summary>
    public static void Draw(SvgWriter writer, IReadOnlyList<FlowKeyItem> items, double x, double y, double width)
    {
        writer.Box(x, y, width, 1, 0, "flow-rule")
            .Text(x, y + HeadingLine, DiagramText.Format(DrawingMessage.KeyHeading), "pic-group", "start");
        foreach (var item in items)
        {
            double left = x + item.X, middle = y + item.Y + FlowKeyLayout.Row / 2;
            Sample(writer, item, left, middle);
            writer.Text(left + item.Sample + FlowKeyLayout.Words, middle + WordDrop, item.Words, "pic-means",
                "start");
        }
    }

    /// <summary>An item's sample, by what the item explains, its left at a point and centred on a height.</summary>
    /// <remarks>A switch on the item's kind, one arm a kind (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, FlowKeyItem item, double x, double y)
    {
        switch (item.Kind)
        {
            // A tone: a small badge filled with it, as a box's badge is.
            case FlowKeyKind.Tone:
                writer.Box(x, y - item.Sample / 2, item.Sample, item.Sample, BadgeCorner,
                    $"pic-fill{PictureParts.Tone(item.Tone)}");
                break;
            // A shape: the shape itself, small.
            case FlowKeyKind.Shape:
                Shape(writer, item, x, y);
                break;
            // What a line's colour says: a short line with its arrow, marked as the key's so it is told from a
            // real one.
            default:
                string line = $"M{SvgWriter.N(x)} {SvgWriter.N(y)}H{SvgWriter.N(x + item.Sample)}";
                writer.Path(line, "flow-key pic-line pic-edge", true);
                break;
        }
    }

    /// <summary>A shape's sample: a pill as a rounded box, a decision or a slanted box by its outline.</summary>
    private static void Shape(SvgWriter writer, FlowKeyItem item, double x, double y)
    {
        var small = new PlacedNode(new GraphNode("", "", Shape: item.Shape), x, y - Tall / 2, item.Sample, Tall);
        const string look = "pic-panel flow-shape pic-edge";
        if (NodeShapes.PathOf(small) is { } outline) writer.Path(outline, look, false);
        else writer.Box(small.X, small.Y, small.W, small.H, Tall / 2, look);
    }
}
