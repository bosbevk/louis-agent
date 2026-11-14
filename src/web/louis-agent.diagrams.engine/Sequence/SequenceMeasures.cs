using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Sequence.Placed;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Engine.Sequence;

/// <summary>
/// The measures of a sequence, as one value the layout, the drawing and the checks read: how wide a head is for its
/// name, how a message's words break and where they stand above their line, how tall a note and a block's tab are.
/// </summary>
/// <remarks>
/// A Parameter Object (Fowler, <i>Introduce Parameter Object</i>), Louis's pick of three (F134.10). The layout is
/// handed the numbers and holds none of its own, so a look that needs other numbers is another instance of this
/// record and no second layout, and two looks can't drift apart in how they space lifelines or frame a block.
/// <see cref="Marquee"/> is the one look there is.
/// </remarks>
public sealed record SequenceMeasures
{
    /// <summary>The word on an alt's divider, in a pill like a block's kind.</summary>
    public const string ElseWord = "else";

    /// <summary>
    /// The cards' look (F134.10, Louis's pick "Marquee"): a head is a badge with its name under it and no box, so
    /// it is as wide as the name alone and tall for the badge. The words are in the sans font and stand above
    /// their line. A block's frame reaches well left of what it holds, for its tab.
    /// </summary>
    public static SequenceMeasures Marquee { get; } = new();

    /// <summary>A message's words break at this many characters (Claude's pick Q2).</summary>
    public int MessageWidth { get; init; } = 40;

    /// <summary>A head's name breaks at this many characters, so a long name doesn't spread its column.</summary>
    public int HeadLabelWidth { get; init; } = 24;

    /// <summary>A message's line: its height.</summary>
    public double LineStep { get; init; } = 15;

    /// <summary>The room above a message's first line.</summary>
    public double TextTop { get; init; } = 8;

    /// <summary>The room between a message's last line and its arrow.</summary>
    public double ArrowGap { get; init; } = 6;

    /// <summary>The words' room around their ink.</summary>
    public double WordPad { get; init; } = 8;

    /// <summary>How far a message to oneself turns out of its lifeline.</summary>
    public double SelfOut { get; init; } = 34;

    /// <summary>How far down a message to oneself comes back.</summary>
    public double SelfDrop { get; init; } = 18;

    /// <summary>
    /// Half of what a head is taller than its name's lines: the badge over the name, and the room round both.
    /// </summary>
    public double HeadPad { get; init; } = 32;

    /// <summary>How wide a character of a head's name is taken to be.</summary>
    public double HeadChar { get; init; } = 9.4;

    /// <summary>A line of a head's name.</summary>
    public double HeadLine { get; init; } = 20;

    /// <summary>The room under a row's arrow, or under its note.</summary>
    public double RowEnd { get; init; } = 22;

    /// <summary>A line of a note's words.</summary>
    public double NoteLine { get; init; } = 16;

    /// <summary>The room above and under a note's words.</summary>
    public double NotePad { get; init; } = 9;

    /// <summary>A head's width at the least, so a short name still has room for its badge.</summary>
    public double HeadMinWidth { get; init; } = 64;

    /// <summary>How far a note reaches past the outermost lifelines it stands over.</summary>
    public double NoteReach { get; init; } = 24;

    /// <summary>A note's room above.</summary>
    public double NoteTop { get; init; } = 4;

    /// <summary>
    /// How far a block's frame reaches left of what it holds, so a nested one sits inside, with room for its tab.
    /// </summary>
    public double InsetLeft { get; init; } = 62;

    /// <summary>How far a block's frame reaches right of what it holds.</summary>
    public double InsetRight { get; init; } = 22;

    /// <summary>The room round a block's tab's words.</summary>
    public double TabPad { get; init; } = 9.5;

    /// <summary>The room under a block's frame, before what comes next.</summary>
    public double BlockGap { get; init; } = 12;

    /// <summary>How far inside its frame a block's tab starts.</summary>
    public double TabLeft { get; init; } = 14;

    /// <summary>The room under a block's last row.</summary>
    public double BlockEnd { get; init; } = 8;

    /// <summary>The icon a head wears: its own, or a person's for an actor that names none.</summary>
    public static DiagramIcon? IconOf(Participant participant) =>
        participant.Icon ?? (participant.Actor ? DiagramIcon.User : null);

    /// <summary>
    /// A message's words, broken into lines: at each written break, then where a line grows too long.
    /// </summary>
    public IReadOnlyList<string> Lines(string text) => Broken(text, MessageWidth);

    /// <summary>A head's name, broken into lines the same way.</summary>
    public IReadOnlyList<string> HeadLines(Participant participant) => Broken(participant.Label, HeadLabelWidth);

    /// <summary>How wide a head is for its name. Its icon stands over the name and takes no room beside it.</summary>
    public double HeadWidth(IReadOnlyList<string> lines) =>
        Math.Max(HeadMinWidth, lines.Max(line => line.Length) * HeadChar);

    /// <summary>How tall a head is for its name's lines.</summary>
    public double HeadHeight(IReadOnlyList<string> lines) => lines.Count * HeadLine + 2 * HeadPad;

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
    public double NoteHeight(IReadOnlyList<string> lines) => lines.Count * NoteLine + 2 * NotePad;

    /// <summary>The panel a note stands on, centred over the lifelines it names.</summary>
    public (double X, double Y, double W, double H) NoteBox(PlacedRow row)
    {
        double width = NoteWidth(row.Lines, row.ToX - row.FromX);
        return ((row.FromX + row.ToX) / 2 - width / 2, row.Y + NoteTop, width, NoteHeight(row.Lines));
    }

    /// <summary>How tall a block's tab is, or an else's divider with its label, for the label's lines.</summary>
    public double TabHeight(IReadOnlyList<string> lines) => lines.Count * LineStep + 2 * TabPad + 4;

    /// <summary>How wide the pill naming a block's kind is.</summary>
    public double KindWidth(BlockKind kind) => kind.Wire().Length * FlowText.WordChar + 2 * WordPad;

    /// <summary>
    /// How wide a frame must be for its tab: from where the tab starts, the kind's pill and the label beside it.
    /// </summary>
    public double TabWidth(BlockKind kind, IReadOnlyList<string> lines) =>
        TabLeft + KindWidth(kind) + WordsWidth(lines) + TabPad;

    /// <summary>
    /// How wide a frame must be for a divider: from where the tab starts, its pill and its case's label beside it.
    /// </summary>
    public double ElseWidth(IReadOnlyList<string> lines) =>
        TabLeft + WordsWidth([ElseWord]) + WordsWidth(lines) + TabPad;

    /// <summary>
    /// Words broken at each written <c>\n</c>, as a flow's label is, then each part wrapped at a width.
    /// </summary>
    private static IReadOnlyList<string> Broken(string text, int width) =>
        text.Split('\n').SelectMany(part => TextWrap.Lines(part, width)).ToList();
}
