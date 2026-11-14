using LouisAgent.Diagrams.Classes.Keys;
using LouisAgent.Diagrams.Classes.Marks;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts.Types;

/// <summary>
/// Draws a class chart's key (F134.11): a faint rule, its heading, then each item where the layout put it, a
/// small sample of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A link's sample is a short line drawn by the code that draws the real one, with the same ends, so the key can
/// never show a link other than the picture's own. A sample stands still: it has no dots.
/// </remarks>
internal static class ClassesKey
{
    /// <summary>
    /// Where the heading's words stand under the rule, and how far under a row's middle an item's do.
    /// </summary>
    private const double HeadingLine = 22, WordDrop = 4.5;

    /// <summary>A tone's swatch and its corner, a letter's small badge and its corner, and its letter's drop.</summary>
    private const double Swatch = 18, SwatchCorner = 6, Badge = 20, LetterDrop = 4;

    /// <summary>The key for a picture that wide, from its own top-left corner.</summary>
    public static SvgWriter Of(IReadOnlyList<ClassKeyItem> items, double width)
    {
        var writer = new SvgWriter();
        writer.Box(0, 0, width, 1, 0, "cls-rule")
            .Text(0, HeadingLine, DiagramText.Format(DiagramMessage.KeyHeading), "pic-group", "start");
        foreach (var item in items)
        {
            double middle = item.Y + ClassKeyLayout.Row / 2;
            Sample(writer, item, item.X, middle);
            writer.Text(item.X + item.Sample + ClassKeyLayout.Words, middle + WordDrop, item.Words, "cls-key",
                "start");
        }

        return writer;
    }

    /// <summary>An item's sample, by what the item explains, its left at a point and centred on a height.</summary>
    /// <remarks>A switch on the item's kind, one arm a kind (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, ClassKeyItem item, double x, double y)
    {
        switch (item.Kind)
        {
            // A tone: a small badge filled with it.
            case ClassKeyKind.Tone:
                writer.Box(x, y - Swatch / 2, Swatch, Swatch, SwatchCorner,
                    "cls-badge" + PictureParts.Tone(item.Tone));
                break;
            // A letter: in a small outlined badge, since the badge's colour is the tone's to say.
            case ClassKeyKind.Mark:
                writer.Box(x, y - Badge / 2, Badge, Badge, SwatchCorner, "cls-key-mark")
                    .Text(x + Badge / 2, y + LetterDrop, TypeMarks.Letter(item.Mark ?? TypeMark.Class),
                        "cls-key-letter");
                break;
            // A kind of link: a short line of it, with its ends.
            default:
                string path = $"M{SvgWriter.N(x)} {SvgWriter.N(y)}H{SvgWriter.N(x + item.Sample)}";
                RelationLines.Line(writer, path, item.Link ?? RelationKind.Uses);
                break;
        }
    }
}
