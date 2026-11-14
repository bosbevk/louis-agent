using LouisAgent.Diagrams.Flow.Keys;
using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts;

/// <summary>
/// Draws a flow's key (F134.8): its heading, then each item where the layout put it, a small sample of the thing
/// with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the shapes and classes of the real thing, so the key can never show a tone or a shape
/// that looks other than the picture's own.
/// </remarks>
internal static class FlowKey
{
    /// <summary>How wide and how tall a sample is.</summary>
    private const double Wide = 26, Tall = 16;

    /// <summary>The key, its top-left corner at a point.</summary>
    public static void Draw(SvgWriter writer, IReadOnlyList<FlowKeyItem> items, double x, double y)
    {
        writer.Text(x, y + 10, DiagramText.Format(DiagramMessage.KeyHeading), "pic-group", "start");
        foreach (var item in items)
        {
            double left = x + item.X, middle = y + item.Y + FlowKeyLayout.Row / 2;
            Sample(writer, item, left, middle);
            writer.Text(left + FlowKeyLayout.Words, middle + 4, item.Words, "pic-note", "start");
        }
    }

    /// <summary>An item's sample, by what the item explains.</summary>
    private static void Sample(SvgWriter writer, FlowKeyItem item, double x, double y)
    {
        string tone = PictureParts.Tone(item.Tone);
        switch (item.Kind)
        {
            // A tone: a small panel in it, with its marker, as a component diagram's key shows one.
            case FlowKeyKind.Tone:
                writer.Box(x, y - Tall / 2, Wide, Tall, 5, $"pic-swatch pic-tint pic-edge{tone}")
                    .Box(x + 4, y - 4, 3, 8, 1.5, $"pic-fill{tone}");
                break;
            // A shape: the shape itself, small.
            case FlowKeyKind.Shape:
                Shape(writer, item.Shape, x, y);
                break;
            // What a line's colour says: a short line with its arrow, marked as the key's so it is told from a
            // real one.
            default:
                string line = $"M{SvgWriter.N(x)} {SvgWriter.N(y)}H{SvgWriter.N(x + Wide + 4)}";
                writer.Path(line, "flow-key pic-line pic-edge", true);
                break;
        }
    }

    /// <summary>A shape's sample: a pill as a rounded box, a diamond or a slanted box by its outline.</summary>
    private static void Shape(SvgWriter writer, NodeShape shape, double x, double y)
    {
        var small = new PlacedNode(new GraphNode("", "", Shape: shape), x, y - Tall / 2, Wide + 4, Tall);
        const string look = "pic-swatch pic-tint pic-edge flow-shape";
        if (NodeShapes.PathOf(small) is { } outline) writer.Path(outline, look, false);
        else writer.Box(small.X, small.Y, small.W, small.H, Tall / 2, look);
    }
}
