using LouisAgent.Diagrams.Classes.Marks;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Classes.Keys;

/// <summary>
/// Places a class chart's key under its drawing (F134.11): "how to read it", then an item for each tone a badge
/// wears, each letter a badge shows and each kind of link, only where the picture really has one, side by side
/// across the drawing's width and on to a new row when a row is full.
/// </summary>
/// <remarks>
/// A pure function of the data, as a flow's and a sequence's key are, so a key never explains something the
/// picture doesn't have. A link's kind is in its line and its ends, which say nothing to a reader who doesn't
/// know the convention, so the key is where a diamond and a hollow triangle are named.
/// </remarks>
public static class ClassKeyLayout
{
    /// <summary>The room the key's heading takes, and how tall a row of items is.</summary>
    public const double Heading = 34, Row = 28;

    /// <summary>How far after its sample an item's words start.</summary>
    public const double Words = 10;

    /// <summary>
    /// How wide a character of an item's words is taken to be, generously, since no font is measured, and the room
    /// between two items.
    /// </summary>
    private const double Char = 6.6, Gap = 30;

    /// <summary>How wide a sample is: a tone's swatch, a letter's small badge, and a link's short line.</summary>
    private const double Swatch = 18, Badge = 20, Line = 50;

    /// <summary>The kinds of link in the order the key shows them: the structure first, then what is looser.</summary>
    private static readonly RelationKind[] Links =
    [
        RelationKind.Holds, RelationKind.Inherits, RelationKind.Implements, RelationKind.Creates, RelationKind.Uses
    ];

    /// <summary>The key's items for a chart, across a width, and how tall the key is.</summary>
    public static (IReadOnlyList<ClassKeyItem> Items, double Height) Of(ClassesSpec spec, double width)
    {
        // Side by side, each after the one before, and on to a new row at the left when the width is full.
        var items = new List<ClassKeyItem>();
        double x = 0, y = Heading;
        foreach (var item in Tones(spec).Concat(Marks(spec)).Concat(Kinds(spec)))
        {
            double room = item.Sample + Words + item.Words.Length * Char;
            if (x > 0 && x + room > width) (x, y) = (0, y + Row);
            items.Add(item with { X = x, Y = y });
            x += room + Gap;
        }

        return (items, y + Row);
    }

    /// <summary>
    /// An item for each tone a badge wears, in the tones' own order, with what the tone means. A type that names
    /// no tone wears its kind's, so that is the tone counted.
    /// </summary>
    private static IEnumerable<ClassKeyItem> Tones(ClassesSpec spec) => spec.Types
        .Select(ClassFlow.ToneOf).Where(tone => tone != DiagramTone.Plain).Distinct().Order()
        .Select(tone => new ClassKeyItem(ClassKeyKind.Tone, ToneMeanings.Of(tone), 0, 0, Swatch, tone));

    /// <summary>
    /// An item for each letter a badge shows, in the marks' own order, with what the letter stands for.
    /// </summary>
    private static IEnumerable<ClassKeyItem> Marks(ClassesSpec spec) => spec.Types
        .Select(TypeMarks.Of).Distinct().Order()
        .Select(mark => new ClassKeyItem(ClassKeyKind.Mark, Text(Meaning(mark)), 0, 0, Badge, Mark: mark));

    /// <summary>An item for each kind of link the picture has, the structure first, with the kind's word.</summary>
    private static IEnumerable<ClassKeyItem> Kinds(ClassesSpec spec)
    {
        var has = spec.Relations.Select(relation => relation.Kind).ToHashSet();
        return Links.Where(has.Contains)
            .Select(kind => new ClassKeyItem(ClassKeyKind.Link, Text(Meaning(kind)), 0, 0, Line, Link: kind));
    }

    /// <summary>The message that says what a letter stands for.</summary>
    private static DrawingMessage Meaning(TypeMark mark) => mark switch
    {
        TypeMark.Interface => DrawingMessage.ClassInterface,
        TypeMark.Record => DrawingMessage.ClassRecord,
        TypeMark.Enum => DrawingMessage.ClassEnum,
        TypeMark.Abstract => DrawingMessage.ClassAbstract,
        TypeMark.Static => DrawingMessage.ClassStatic,
        _ => DrawingMessage.ClassPlain
    };

    /// <summary>The message that says a kind of link.</summary>
    private static DrawingMessage Meaning(RelationKind kind) => kind switch
    {
        RelationKind.Holds => DrawingMessage.LinkHolds,
        RelationKind.Inherits => DrawingMessage.LinkInherits,
        RelationKind.Implements => DrawingMessage.LinkImplements,
        RelationKind.Creates => DrawingMessage.LinkCreates,
        _ => DrawingMessage.LinkUses
    };

    /// <summary>A message's words, which have no holes.</summary>
    private static string Text(DrawingMessage message) => DiagramText.Format(message);
}
