using LouisAgent.Choices;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Sequence;

/// <summary>
/// The measures of a sequence's words, in one place for the layout, the renderer and the checks: how wide a head is
/// for its label, how a message's words break and where they stand above their arrow.
/// </summary>
/// <remarks>
/// Every word is in the code font, so a line's width is its length times one number, as in the flows; the heads
/// take the flows' box measures, so a participant looks like a box in a flow.
/// </remarks>
public static class SequenceText
{
    /// <summary>A message's words break at this many characters (Claude's pick Q2).</summary>
    public const int MessageWidth = 40;

    /// <summary>A head's label breaks at this many characters, so a long name doesn't spread its column.</summary>
    public const int HeadLabelWidth = 24;

    /// <summary>A message's line: its height, and the room above the first and below the last.</summary>
    public const double LineStep = 14, TextTop = 8, ArrowGap = 8;

    /// <summary>The words' room around their ink, and where a message to oneself starts and how far it turns out.</summary>
    public const double WordPad = 8, SelfOut = 28, SelfDrop = 16;

    /// <summary>A head's height at the least, the height of a flow's box; a longer label adds a line each.</summary>
    public const double HeadMin = 52, HeadPad = 18;

    /// <summary>A head's width at the least, so a short name still reads as a box.</summary>
    public const double HeadMinWidth = 120;

    /// <summary>A message's words, broken into lines.</summary>
    public static IReadOnlyList<string> Lines(string text) => TextWrap.Lines(text, MessageWidth);

    /// <summary>A head's label, broken into lines.</summary>
    public static IReadOnlyList<string> HeadLines(Participant participant) =>
        TextWrap.Lines(participant.Label, HeadLabelWidth);

    /// <summary>How wide a head is for its label and its icon.</summary>
    public static double HeadWidth(Participant participant, IReadOnlyList<string> lines)
    {
        double label = lines.Max(line => line.Length) * GraphLayout.LabelCharWidth + 2 * GraphLayout.LabelPad;
        double icon = IconOf(participant) is null ? 0 : FlowText.IconRoom;
        return Math.Max(HeadMinWidth, label + icon);
    }

    /// <summary>The icon a head wears: its own, or a person's for an actor that names none.</summary>
    public static DiagramIcon? IconOf(Participant participant) =>
        participant.Icon ?? (participant.Actor ? DiagramIcon.User : null);

    /// <summary>How tall a head is for its label's lines.</summary>
    public static double HeadHeight(IReadOnlyList<string> lines) =>
        Math.Max(HeadMin, lines.Count * FlowText.LabelStep + 2 * HeadPad);

    /// <summary>How wide a message's words are, their room included.</summary>
    public static double WordsWidth(IReadOnlyList<string> lines) =>
        lines.Max(line => line.Length) * FlowText.WordChar + 2 * WordPad;

    /// <summary>
    /// The ground a message's words take: centred between its lifelines, or for a message to oneself, just right of
    /// its lifeline beside the turn.
    /// </summary>
    public static (double X, double Y, double W, double H) WordBox(PlacedRow row)
    {
        double width = WordsWidth(row.Lines), height = row.Lines.Count * LineStep + WordPad;
        double left = row.Self ? row.FromX + SelfOut + WordPad : (row.FromX + row.ToX) / 2 - width / 2;
        return (left, row.Y + TextTop - WordPad / 2, width, height);
    }

    /// <summary>How far a note reaches past the outermost lifelines it stands over, and its room above.</summary>
    public const double NoteReach = 16, NoteTop = 4;

    /// <summary>How wide a note is: its words, or the span of its lifelines and a reach each side, the wider.</summary>
    public static double NoteWidth(IReadOnlyList<string> lines, double span) =>
        Math.Max(WordsWidth(lines), span + 2 * NoteReach);

    /// <summary>How tall a note's panel is for its lines.</summary>
    public static double NoteHeight(IReadOnlyList<string> lines) => lines.Count * LineStep + 2 * WordPad;

    /// <summary>
    /// A block's frame: how far it reaches past what it holds (so a nested one sits inside), the room round its tab's
    /// words, and the room under its last row.
    /// </summary>
    public const double Inset = 14, TabPad = 6, BlockEnd = 8;

    /// <summary>How tall a block's tab is, or an else's divider with its label, for the label's lines.</summary>
    public static double TabHeight(IReadOnlyList<string> lines) => lines.Count * LineStep + 2 * TabPad + 4;

    /// <summary>How wide the chip naming a block's kind is.</summary>
    public static double KindWidth(BlockKind kind) => kind.Wire().Length * FlowText.WordChar + 2 * WordPad;

    /// <summary>How wide a frame must be for its tab: the kind's chip and the label beside it.</summary>
    public static double TabWidth(BlockKind kind, IReadOnlyList<string> lines) =>
        KindWidth(kind) + WordsWidth(lines) + TabPad;

    /// <summary>The word on an alt's divider, in a chip like a block's kind.</summary>
    public const string ElseWord = "else";

    /// <summary>How wide a frame must be for a divider: its chip and its case's label beside it.</summary>
    public static double ElseWidth(IReadOnlyList<string> lines) => WordsWidth([ElseWord]) + WordsWidth(lines) + TabPad;

    /// <summary>The panel a note stands on, centred over the lifelines it names.</summary>
    public static (double X, double Y, double W, double H) NoteBox(PlacedRow row)
    {
        double width = NoteWidth(row.Lines, row.ToX - row.FromX);
        return ((row.FromX + row.ToX) / 2 - width / 2, row.Y + NoteTop, width, NoteHeight(row.Lines));
    }
}
