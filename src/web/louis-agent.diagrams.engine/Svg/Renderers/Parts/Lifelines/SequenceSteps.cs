using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence;
using LouisAgent.Diagrams.Engine.Sequence.Placed;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;

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
    /// middle of its ring, how round a turn's corners are, how far after a turn its words start, how far under
    /// the middle of a turn its words' middle line stands, and how far under a note's padding its first line
    /// stands.
    /// </summary>
    private const double WordRise = 9, CountDrop = 3.5, Round = 8, TurnGap = 12, TurnDrop = 4, NoteBase = 12;

    /// <summary>A note's corner.</summary>
    private const double Corner = 12;

    private static readonly SequenceMeasures Measures = SequenceMeasures.Marquee;

    /// <summary>The tone of a call's arrowhead, where its beam ends, and of a turn's, which is one colour.</summary>
    private static readonly string Arrives = DiagramTone.Blue.Wire(), Turns = DiagramTone.Violet.Wire();

    /// <summary>One step, where the layout put it.</summary>
    /// <remarks>A switch on the row's shape, one arm a kind (ADR 0051).</remarks>
    public static void Draw(SvgWriter writer, PlacedRow row, IGeometry geometry)
    {
        switch (row)
        {
            case { Step: NoteStep }:
                Note(writer, row, geometry);
                break;
            case { Self: true }:
                Turn(writer, row);
                break;
            default:
                Across(writer, row, geometry);
                break;
        }
    }

    /// <summary>
    /// A call's line from its sender to its receiver: the glow, the beam, the arrowhead where it arrives, and the
    /// dots that run along it when it moves. The glow and the beam are two strips on the line's height.
    /// </summary>
    public static void Beam(SvgWriter writer, IGeometry geometry, double from, double to, double y, bool dots)
    {
        double way = Math.Sign(to - from), left = Math.Min(from, to), wide = Math.Abs(to - from);
        // A call to the left is filled with the beam's mirror, so violet stays at its sender.
        string back = way < 0 ? " back" : "";
        var glow = geometry.Box(left, 0, wide, Glow).OnLine(y);
        var beam = geometry.Box(way < 0 ? left + Tip : left, 0, wide - Tip, Line).OnLine(y);
        writer.Box(glow, 0, "seq-beam seq-beam-glow" + back).Box(beam, 0, "seq-beam" + back);
        if (dots) writer.Path(PathWriter.Level(from, to - way * Tip, y), "pic-march", false);
        writer.Path(PathWriter.Level(to - way * Tip, to, y), "seq-tip", true, tone: Arrives);
    }

    /// <summary>An answer's line: thin, dashed and quiet, with a plain arrowhead. It stands still.</summary>
    public static void Dashes(SvgWriter writer, double from, double to, double y) =>
        writer.Path(PathWriter.Level(from, to, y), "seq-answer", true);

    /// <summary>A call or an answer between two lifelines: its line, its node, and its words above the line.</summary>
    private static void Across(SvgWriter writer, PlacedRow row, IGeometry geometry)
    {
        bool call = row.Step is MessageStep;
        if (call) Beam(writer, geometry, row.FromX, row.ToX, row.ArrowY, dots: true);
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
        string path = new PathWriter().From(x, y).Across(far - Round).Curve(far, y, far, y + Round)
            .Down(under - Round).Curve(far, under, far - Round, under).Across(x).Text;
        writer.Path(path, call ? "seq-turn" : "seq-answer", true, tone: call ? Turns : null);
        if (call) writer.Path(path, "pic-march", false);
        Start(writer, row, call);

        // The words' lines centred on the turn's height, starting after it.
        double middle = (y + under) / 2 + TurnDrop - (row.Lines.Count - 1) * Measures.LineStep / 2;
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
    private static void Note(SvgWriter writer, PlacedRow row, IGeometry geometry)
    {
        var note = Measures.NoteBox(row, geometry);
        writer.Box(note, Corner, "seq-cover").Box(note, Corner, "mrq-glass");
        for (int at = 0; at < row.Lines.Count; at++)
            writer.Text(note.Middle.X, note.Top + Measures.NotePad + NoteBase + at * Measures.NoteLine, row.Lines[at],
                "seq-note-words");
    }

    /// <summary>The look of a call's words, bold, or an answer's, quiet.</summary>
    private static string Words(bool call) => call ? "seq-call-words" : "seq-answer-words";
}
