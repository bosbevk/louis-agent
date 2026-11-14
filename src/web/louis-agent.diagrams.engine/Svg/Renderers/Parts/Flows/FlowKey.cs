using LouisAgent.Diagrams.Engine.Flow.Keys;
using LouisAgent.Diagrams.Engine.Flow.Shapes;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Keys;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;

/// <summary>
/// Draws a flow's key (F134.8): a faint rule, its heading, then each item where the layout put it, a small sample
/// of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the shapes and classes of the real thing, so the key can never show a tone or a shape
/// that looks other than the picture's own. A tone's sample is a small badge filled with it, since a box's badge
/// is what says its tone. The rule, the rows and the words are every key's (<see cref="KeyParts"/>, F134.12,
/// Louis of the flow's key: "do not look like the other graphs"), and this says only what a flow's samples are.
/// </remarks>
internal static class FlowKey
{
    /// <summary>How tall a shape's sample is, and a tone's badge's corner.</summary>
    private const double Tall = 16, BadgeCorner = 6;

    /// <summary>The key for a picture that wide, at its left, its top at a height.</summary>
    public static void Draw(SvgWriter writer, IReadOnlyList<FlowKeyItem> items, IGeometry geometry, double top,
        double width)
    {
        KeyParts.Head(writer, geometry, top, width);
        foreach (var item in items)
        {
            var row = KeyParts.Row(geometry, item.X, top + item.Y, item.Sample);
            Sample(writer, item, row, geometry);
            KeyParts.Words(writer, row, item.Words, "pic-means");
        }
    }

    /// <summary>An item's sample, by what the item explains, in its row.</summary>
    /// <remarks>A switch on the item's kind, one arm a kind (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, FlowKeyItem item, IBox row, IGeometry geometry)
    {
        switch (item.Kind)
        {
            // A tone: a small badge filled with it, as a box's badge is.
            case FlowKeyKind.Tone:
                var badge = KeyParts.Sample(geometry, row, item.Sample, item.Sample);
                writer.Box(badge, BadgeCorner, $"pic-fill{PictureParts.Tone(item.Tone)}");
                break;
            // A shape: the shape itself, small.
            case FlowKeyKind.Shape:
                Shape(writer, item, KeyParts.Sample(geometry, row, item.Sample, Tall));
                break;
            // What a line's colour says: a short line with its arrow, marked as the key's so it is told from a
            // real one.
            default:
                writer.Path(KeyParts.Line(row), "flow-key pic-line pic-edge", true);
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
