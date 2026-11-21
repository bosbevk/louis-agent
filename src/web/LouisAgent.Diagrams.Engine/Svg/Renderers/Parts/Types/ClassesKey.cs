using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Classes.Keys;
using LouisAgent.Diagrams.Engine.Classes.Marks;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Keys;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Types;

/// <summary>
/// Draws a class chart's key (F134.11): a faint rule, its heading, then each item where the layout put it, a
/// small sample of the thing with what it means beside it.
/// </summary>
/// <remarks>
/// A link's sample is a short line drawn by the code that draws the real one, with the same ends, so the key can
/// never show a link other than the picture's own. A sample stands still: it has no dots. The rule, the rows and
/// the words are every key's (<see cref="KeyParts"/>).
/// </remarks>
internal static class ClassesKey
{
    /// <summary>
    /// The corner of a tone's swatch and of a letter's small badge, and how far under its row's middle a letter
    /// stands. How big each is, is the layout's to say: a sample is a square as wide as its row.
    /// </summary>
    private const double SwatchCorner = 6, LetterDrop = 4;

    /// <summary>The key for a picture that wide, from its own top-left corner.</summary>
    public static SvgWriter Of(IReadOnlyList<ClassKeyItem> items, double width, IGeometry geometry)
    {
        var writer = new SvgWriter();
        KeyParts.Head(writer, geometry, 0, width);
        foreach (var item in items)
        {
            var row = KeyParts.Row(geometry, item.X, item.Y, item.Sample);
            Sample(writer, item, row, geometry);
            KeyParts.Words(writer, row, item.Words, "pic-means");
        }

        return writer;
    }

    /// <summary>An item's sample, by what the item explains, in its row.</summary>
    /// <remarks>A switch on the item's kind, one arm a kind (ADR 0051).</remarks>
    private static void Sample(SvgWriter writer, ClassKeyItem item, IBox row, IGeometry geometry)
    {
        switch (item.Kind)
        {
            // A tone: a small badge filled with it.
            case ClassKeyKind.Tone:
                var swatch = KeyParts.Sample(geometry, row, row.Width, row.Width);
                writer.Box(swatch, SwatchCorner, "cls-badge" + PictureParts.Tone(item.Tone));
                break;
            // A letter: in a small outlined badge, since the badge's colour is the tone's to say.
            case ClassKeyKind.Mark:
                var badge = KeyParts.Sample(geometry, row, row.Width, row.Width);
                writer.Box(badge, SwatchCorner, "cls-key-mark").Text(badge.Middle.X, row.Middle.Y + LetterDrop,
                    TypeMarks.Letter(item.Mark ?? TypeMark.Class), "cls-key-letter");
                break;
            // A kind of link: a short line of it, with its ends.
            default:
                RelationLines.Line(writer, KeyParts.Line(row), item.Link ?? RelationKind.Uses);
                break;
        }
    }
}
