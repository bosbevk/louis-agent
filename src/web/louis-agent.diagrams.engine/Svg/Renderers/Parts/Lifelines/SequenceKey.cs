using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Keys;
using LouisAgent.Diagrams.Engine.Sequence.Keys;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// Draws a sequence's key (F134.10): a faint rule, its heading, then each item where the layout put it, a small
/// sample of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the shapes and classes of the real thing, a call's and an answer's by the very code
/// that draws them in the picture, so the key can never show a look other than the picture's own. A sample
/// stands still: a call's has no dots. The rule, the rows and the words are every key's (<see cref="KeyParts"/>).
/// </remarks>
internal static class SequenceKey
{
    /// <summary>
    /// A tone's swatch's corner, how tall a note's or a block's sample is, a note's corner, and how far under
    /// its row's middle a block's kind stands in its pill.
    /// </summary>
    private const double SwatchCorner = 6, Tall = 20, NoteCorner = 8, KindDrop = 4;

    /// <summary>The key for a picture that wide, from its own top-left corner.</summary>
    public static SvgWriter Of(IReadOnlyList<SequenceKeyItem> items, double width, IGeometry geometry)
    {
        var writer = new SvgWriter();
        KeyParts.Head(writer, geometry, 0, width, "seq-rule");
        foreach (var item in items)
        {
            var row = KeyParts.Row(geometry, item.X, item.Y, item.Sample);
            Sample(writer, item, row, geometry);
            KeyParts.Words(writer, row, item.Words, "seq-key");
        }

        return writer;
    }

    /// <summary>An item's sample, by what the item explains, in its row.</summary>
    /// <remarks>A switch on the item's kind, one arm a kind (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, SequenceKeyItem item, IBox row, IGeometry geometry)
    {
        // A line's sample starts at its node, a node's size in from the row's left, and ends at the row's right.
        double start = row.Left + SequenceSteps.Node, y = row.Middle.Y;
        switch (item.Kind)
        {
            // A tone: a small badge filled with it, a square as wide as its row.
            case SequenceKeyKind.Tone:
                var swatch = KeyParts.Sample(geometry, row, row.Width, row.Width);
                writer.Box(swatch, SwatchCorner, "seq-badge" + PictureParts.Tone(item.Tone));
                break;
            // A call and an answer: a short one of each, from its node to its arrowhead.
            case SequenceKeyKind.Call:
                SequenceSteps.Beam(writer, geometry, start, row.Right, y, dots: false);
                writer.Circle(start, y, SequenceSteps.Node, "mrq-node");
                break;
            case SequenceKeyKind.Answer:
                SequenceSteps.Dashes(writer, start, row.Right, y);
                writer.Circle(start, y, SequenceSteps.Node, "seq-quiet-node");
                break;
            // A note: a small panel of its glass.
            case SequenceKeyKind.Note:
                writer.Box(KeyParts.Sample(geometry, row, row.Width, Tall), NoteCorner, "seq-swatch");
                break;
            // A block: its kind in the pill a frame shows it in.
            default:
                var pill = KeyParts.Sample(geometry, row, row.Width, Tall);
                writer.Box(pill, pill.HalfHeight, "seq-kind")
                    .Text(pill.Middle.X, y + KindDrop, item.Block?.Wire() ?? "", "mrq-code");
                break;
        }
    }
}
