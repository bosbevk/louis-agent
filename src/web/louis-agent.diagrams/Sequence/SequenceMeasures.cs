using LouisAgent.Choices;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Sequence;

/// <summary>
/// The measures of a sequence, as one value the layout, the renderer and the checks are handed: how wide a head is
/// for its label, how a message's words break and where they stand above their arrow, how tall a note and a block's
/// tab are.
/// </summary>
/// <remarks>
/// A Parameter Object (Fowler, <i>Introduce Parameter Object</i>), Louis's pick of three (F134.10). The numbers were
/// constants every part read. As one value, the same layout places a sequence by today's numbers or by another
/// look's, so a new look needs no second layout, and the two can't drift apart in how they space lifelines or frame
/// a block. <see cref="Boxed"/> is today's: every word in the code font, so a line's width is its length times one
/// number, and a head takes a flow's box measures, so a participant looks like a box in a flow.
/// </remarks>
public sealed record SequenceMeasures
{
    /// <summary>The word on an alt's divider, in a chip like a block's kind.</summary>
    public const string ElseWord = "else";

    /// <summary>Today's measures: a head as a flow's box, and every word in the code font.</summary>
    public static SequenceMeasures Boxed { get; } = new();

    /// <summary>A message's words break at this many characters (Claude's pick Q2).</summary>
    public int MessageWidth { get; init; } = 40;

    /// <summary>A head's label breaks at this many characters, so a long name doesn't spread its column.</summary>
    public int HeadLabelWidth { get; init; } = 24;

    /// <summary>A message's line: its height.</summary>
    public double LineStep { get; init; } = 14;

    /// <summary>The room above a message's first line.</summary>
    public double TextTop { get; init; } = 8;

    /// <summary>The room between a message's last line and its arrow.</summary>
    public double ArrowGap { get; init; } = 8;

    /// <summary>The words' room around their ink.</summary>
    public double WordPad { get; init; } = 8;

    /// <summary>How far a message to oneself turns out of its lifeline.</summary>
    public double SelfOut { get; init; } = 28;

    /// <summary>How far down a message to oneself comes back.</summary>
    public double SelfDrop { get; init; } = 16;

    /// <summary>A head's height at the least, the height of a flow's box; a longer label adds a line each.</summary>
    public double HeadMin { get; init; } = 52;

    /// <summary>The room above and under a head's label.</summary>
    public double HeadPad { get; init; } = 18;

    /// <summary>A head's width at the least, so a short name still reads as a box.</summary>
    public double HeadMinWidth { get; init; } = 120;

    /// <summary>How far a note reaches past the outermost lifelines it stands over.</summary>
    public double NoteReach { get; init; } = 16;

    /// <summary>A note's room above.</summary>
    public double NoteTop { get; init; } = 4;

    /// <summary>How far a block's frame reaches past what it holds, so a nested one sits inside.</summary>
    public double Inset { get; init; } = 14;

    /// <summary>The room round a block's tab's words.</summary>
    public double TabPad { get; init; } = 6;

    /// <summary>The room under a block's last row.</summary>
    public double BlockEnd { get; init; } = 8;

    /// <summary>The icon a head wears: its own, or a person's for an actor that names none.</summary>
    public static DiagramIcon? IconOf(Participant participant) =>
        participant.Icon ?? (participant.Actor ? DiagramIcon.User : null);

    /// <summary>A message's words, broken into lines: at each written break, then where a line grows too long.</summary>
    public IReadOnlyList<string> Lines(string text) => Broken(text, MessageWidth);

    /// <summary>A head's label, broken into lines the same way.</summary>
    public IReadOnlyList<string> HeadLines(Participant participant) => Broken(participant.Label, HeadLabelWidth);

    /// <summary>How wide a head is for its label and its icon.</summary>
    public double HeadWidth(Participant participant, IReadOnlyList<string> lines)
    {
        double label = lines.Max(line => line.Length) * GraphLayout.LabelCharWidth + 2 * GraphLayout.LabelPad;
        double icon = IconOf(participant) is null ? 0 : FlowText.IconRoom;
        return Math.Max(HeadMinWidth, label + icon);
    }

    /// <summary>How tall a head is for its label's lines.</summary>
    public double HeadHeight(IReadOnlyList<string> lines) =>
        Math.Max(HeadMin, lines.Count * FlowText.LabelStep + 2 * HeadPad);

    /// <summary>How wide a message's words are, their room included.</summary>
    public double WordsWidth(IReadOnlyList<string> lines) =>
        lines.Select(line => line.Length).DefaultIfEmpty(0).Max() * FlowText.WordChar + 2 * WordPad;

    /// <summary>
    /// The ground a message's words take: centred between its lifelines, or for a message to oneself, just right of
    /// its lifeline beside the turn.
    /// </summary>
    public (double X, double Y, double W, double H) WordBox(PlacedRow row)
    {
        double width = WordsWidth(row.Lines), height = row.Lines.Count * LineStep + WordPad;
        double left = row.Self ? row.FromX + SelfOut + WordPad : (row.FromX + row.ToX) / 2 - width / 2;
        return (left, row.Y + TextTop - WordPad / 2, width, height);
    }

    /// <summary>How wide a note is: its words, or the span of its lifelines and a reach each side, the wider.</summary>
    public double NoteWidth(IReadOnlyList<string> lines, double span) =>
        Math.Max(WordsWidth(lines), span + 2 * NoteReach);

    /// <summary>How tall a note's panel is for its lines.</summary>
    public double NoteHeight(IReadOnlyList<string> lines) => lines.Count * LineStep + 2 * WordPad;

    /// <summary>The panel a note stands on, centred over the lifelines it names.</summary>
    public (double X, double Y, double W, double H) NoteBox(PlacedRow row)
    {
        double width = NoteWidth(row.Lines, row.ToX - row.FromX);
        return ((row.FromX + row.ToX) / 2 - width / 2, row.Y + NoteTop, width, NoteHeight(row.Lines));
    }

    /// <summary>How tall a block's tab is, or an else's divider with its label, for the label's lines.</summary>
    public double TabHeight(IReadOnlyList<string> lines) => lines.Count * LineStep + 2 * TabPad + 4;

    /// <summary>How wide the chip naming a block's kind is.</summary>
    public double KindWidth(BlockKind kind) => kind.Wire().Length * FlowText.WordChar + 2 * WordPad;

    /// <summary>How wide a frame must be for its tab: the kind's chip and the label beside it.</summary>
    public double TabWidth(BlockKind kind, IReadOnlyList<string> lines) =>
        KindWidth(kind) + WordsWidth(lines) + TabPad;

    /// <summary>How wide a frame must be for a divider: its chip and its case's label beside it.</summary>
    public double ElseWidth(IReadOnlyList<string> lines) => WordsWidth([ElseWord]) + WordsWidth(lines) + TabPad;

    /// <summary>Words broken at each written <c>\n</c>, as a flow's label is, then each part wrapped at a width.</summary>
    private static IReadOnlyList<string> Broken(string text, int width) =>
        text.Split('\n').SelectMany(part => TextWrap.Lines(part, width)).ToList();
}
