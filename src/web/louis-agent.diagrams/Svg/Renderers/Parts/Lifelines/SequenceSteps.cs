using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Sequence;
using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Tones;
using static LouisAgent.Diagrams.Svg.SvgWriter;

namespace LouisAgent.Diagrams.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// Draws a sequence's step: a call as a beam with its dots, an answer as a quiet dashed line, a call a
/// participant makes to itself as a turn, each with a node where it leaves and its words, and a note as glass.
/// </summary>
/// <remarks>
/// Every call is in the look's one accent, violet at its sender running to blue at its receiver, whichever way
/// it points. A beam is a thin box and not a line, so its fade has a height to be drawn in, and its arrowhead is
/// on a short path of its own that shows nothing else. The dots are on a third path, since they are hidden for a
/// reader who asked for no motion, and the arrowhead must stay. The key draws its samples with
/// <see cref="Beam"/> and <see cref="Dashes"/>, so a sample can't look other than the real thing.
/// </remarks>
internal static class SequenceSteps
{
    /// <summary>A node where a line leaves its lifeline, and the ring a number stands in.</summary>
    public const double Node = 4.5, Ring = 9;

    /// <summary>
    /// A beam: how tall it and its glow are, and how far short of its end it stops, where the arrowhead takes
    /// over.
    /// </summary>
    private const double Line = 2, Glow = 8, Tip = 8;

    /// <summary>
    /// The words: how far above its line a message's last line stands, how far a number is dropped to sit in the
    /// middle of its ring, how round a turn's corners are, how far after a turn its words start, and how far
    /// under a note's padding its first line stands.
    /// </summary>
    private const double WordRise = 9, CountDrop = 3.5, Round = 8, TurnGap = 12, NoteBase = 12;

    /// <summary>A note's corner.</summary>
    private const double Corner = 12;

    private static readonly SequenceMeasures Measures = SequenceMeasures.Marquee;

    /// <summary>The tone of a call's arrowhead, where its beam ends, and of a turn's, which is one colour.</summary>
    private static readonly string Arrives = DiagramTone.Blue.Wire(), Turns = DiagramTone.Violet.Wire();

    /// <summary>One step, where the layout put it.</summary>
    /// <remarks>A switch on the row's shape, one arm a kind (ADR 0051).</remarks>
    public static void Draw(SvgWriter writer, PlacedRow row)
    {
        switch (row)
        {
            case { Step: NoteStep }:
                Note(writer, row);
                break;
            case { Self: true }:
                Turn(writer, row);
                break;
            default:
                Across(writer, row);
                break;
        }
    }

    /// <summary>
    /// A call's line from its sender to its receiver: the glow, the beam, the arrowhead where it arrives, and the
    /// dots that run along it when it moves.
    /// </summary>
    public static void Beam(SvgWriter writer, double from, double to, double y, bool dots)
    {
        double way = Math.Sign(to - from), left = Math.Min(from, to), wide = Math.Abs(to - from);
        // A call to the left is filled with the beam's mirror, so violet stays at its sender.
        string back = way < 0 ? " back" : "";
        writer.Box(left, y - Glow / 2, wide, Glow, 0, "seq-beam seq-beam-glow" + back)
            .Box(way < 0 ? left + Tip : left, y - Line / 2, wide - Tip, Line, 0, "seq-beam" + back);
        if (dots) writer.Path($"M{N(from)} {N(y)}H{N(to - way * Tip)}", "pic-march", false);
        writer.Path($"M{N(to - way * Tip)} {N(y)}H{N(to)}", "seq-tip", true, tone: Arrives);
    }

    /// <summary>An answer's line: thin, dashed and quiet, with a plain arrowhead. It stands still.</summary>
    public static void Dashes(SvgWriter writer, double from, double to, double y) =>
        writer.Path($"M{N(from)} {N(y)}H{N(to)}", "seq-answer", true);

    /// <summary>A call or an answer between two lifelines: its line, its node, and its words above the line.</summary>
    private static void Across(SvgWriter writer, PlacedRow row)
    {
        bool call = row.Step is MessageStep;
        if (call) Beam(writer, row.FromX, row.ToX, row.ArrowY, dots: true);
        else Dashes(writer, row.FromX, row.ToX, row.ArrowY);
        Start(writer, row, call);

        // The last line stands just above the line, and the ones before it above that.
        double last = row.ArrowY - WordRise, middle = (row.FromX + row.ToX) / 2;
        for (int at = 0; at < row.Lines.Count; at++)
            writer.Text(middle, last - (row.Lines.Count - 1 - at) * Measures.LineStep, row.Lines[at], Words(call));
    }

    /// <summary>
    /// A call a participant makes to itself: out of its lifeline, round and back a little lower, with its words
    /// beside the turn. An answer to oneself is the same turn, dashed and quiet.
    /// </summary>
    private static void Turn(SvgWriter writer, PlacedRow row)
    {
        bool call = row.Step is MessageStep;
        double x = row.FromX, y = row.ArrowY, far = x + Measures.SelfOut, under = y + Measures.SelfDrop;
        string path = $"M{N(x)} {N(y)}H{N(far - Round)}Q{N(far)} {N(y)} {N(far)} {N(y + Round)}" +
                      $"V{N(under - Round)}Q{N(far)} {N(under)} {N(far - Round)} {N(under)}H{N(x)}";
        writer.Path(path, call ? "seq-turn" : "seq-answer", true, tone: call ? Turns : null);
        if (call) writer.Path(path, "pic-march", false);
        Start(writer, row, call);

        // The words' lines centred on the turn's height, starting after it.
        double middle = (y + under) / 2 + 4 - (row.Lines.Count - 1) * Measures.LineStep / 2;
        for (int at = 0; at < row.Lines.Count; at++)
            writer.Text(far + TurnGap, middle + at * Measures.LineStep, row.Lines[at], Words(call), "start");
    }

    /// <summary>
    /// Where a line leaves its lifeline: a node, or in a numbered picture the step's number in a ring. An
    /// answer's is quiet.
    /// </summary>
    private static void Start(SvgWriter writer, PlacedRow row, bool call)
    {
        if (row.Number is not { } number)
        {
            writer.Circle(row.FromX, row.ArrowY, Node, call ? "mrq-node" : "seq-quiet-node");
            return;
        }

        writer.Circle(row.FromX, row.ArrowY, Ring, call ? "seq-ring" : "seq-ring quiet")
            .Text(row.FromX, row.ArrowY + CountDrop, number.ToString(CultureInfo.InvariantCulture), "seq-count");
    }

    /// <summary>
    /// A note: glass over the lifelines it names, on a cover that hides them, its words centred on it.
    /// </summary>
    private static void Note(SvgWriter writer, PlacedRow row)
    {
        var (x, y, w, h) = Measures.NoteBox(row);
        writer.Box(x, y, w, h, Corner, "seq-cover").Box(x, y, w, h, Corner, "mrq-glass");
        for (int at = 0; at < row.Lines.Count; at++)
            writer.Text(x + w / 2, y + Measures.NotePad + NoteBase + at * Measures.NoteLine, row.Lines[at],
                "seq-note-words");
    }

    /// <summary>The look of a call's words, bold, or an answer's, quiet.</summary>
    private static string Words(bool call) => call ? "seq-call-words" : "seq-answer-words";
}
