using LouisAgent.Choices;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sequence.Keys;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// Draws a sequence's key (F134.10): a faint rule, its heading, then each item where the layout put it, a small
/// sample of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A sample is drawn with the shapes and classes of the real thing, a call's and an answer's by the very code
/// that draws them in the picture, so the key can never show a look other than the picture's own. A sample
/// stands still: a call's has no dots.
/// </remarks>
internal static class SequenceKey
{
    /// <summary>
    /// Where the heading's words stand under the rule, and how far under a row's middle an item's do.
    /// </summary>
    private const double HeadingLine = 22, WordDrop = 4.5;

    /// <summary>A tone's swatch and its corner, and how tall a note's or a block's sample is.</summary>
    private const double Swatch = 18, SwatchCorner = 6, Tall = 20, NoteCorner = 8;

    /// <summary>The key for a picture that wide, from its own top-left corner.</summary>
    public static SvgWriter Of(IReadOnlyList<SequenceKeyItem> items, double width)
    {
        var writer = new SvgWriter();
        writer.Box(0, 0, width, 1, 0, "seq-rule")
            .Text(0, HeadingLine, DiagramText.Format(DrawingMessage.KeyHeading), "pic-group", "start");
        foreach (var item in items)
        {
            double middle = item.Y + SequenceKeyLayout.Row / 2;
            Sample(writer, item, item.X, middle);
            writer.Text(item.X + item.Sample + SequenceKeyLayout.Words, middle + WordDrop, item.Words, "seq-key",
                "start");
        }

        return writer;
    }

    /// <summary>An item's sample, by what the item explains, its left at a point and centred on a height.</summary>
    /// <remarks>A switch on the item's kind, one arm a kind (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, SequenceKeyItem item, double x, double y)
    {
        double start = x + SequenceSteps.Node, end = x + item.Sample;
        switch (item.Kind)
        {
            // A tone: a small badge filled with it.
            case SequenceKeyKind.Tone:
                writer.Box(x, y - Swatch / 2, Swatch, Swatch, SwatchCorner,
                    "seq-badge" + PictureParts.Tone(item.Tone));
                break;
            // A call and an answer: a short one of each, from its node to its arrowhead.
            case SequenceKeyKind.Call:
                SequenceSteps.Beam(writer, start, end, y, dots: false);
                writer.Circle(start, y, SequenceSteps.Node, "mrq-node");
                break;
            case SequenceKeyKind.Answer:
                SequenceSteps.Dashes(writer, start, end, y);
                writer.Circle(start, y, SequenceSteps.Node, "seq-quiet-node");
                break;
            // A note: a small panel of its glass.
            case SequenceKeyKind.Note:
                writer.Box(x, y - Tall / 2, item.Sample, Tall, NoteCorner, "seq-swatch");
                break;
            // A block: its kind in the pill a frame shows it in.
            default:
                writer.Box(x, y - Tall / 2, item.Sample, Tall, Tall / 2, "seq-kind")
                    .Text(x + item.Sample / 2, y + 4, item.Block?.Wire() ?? "", "mrq-code");
                break;
        }
    }
}
