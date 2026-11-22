using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;
using LouisAgent.Diagrams.Engine.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Engine.Sequence.Listed;

/// <summary>
/// Walks a sequence's steps down a page: each call, answer and note at the height and in the room it is handed,
/// and each block a frame round its own steps, a level in.
/// </summary>
/// <remarks>
/// It walks the Composite the steps make, as <see cref="SequenceRows"/> does for the wide drawing: a block lays its
/// own steps a level in, then frames what came back, so a frame inside a frame always sits inside its parent and
/// the numbers run on in the order the steps happen. It holds the steps, the frames and the count while it walks,
/// so it is a class rather than a set of functions.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/composite">Refactoring.Guru: Composite</seealso>
internal sealed class StepListRows(
    IReadOnlyDictionary<string, Participant> people,
    bool numbered,
    StepListMeasures measures,
    IGeometry geometry)
{
    /// <summary>The calls, answers and notes, top to bottom.</summary>
    public List<PlacedStep> Steps { get; } = [];

    /// <summary>The blocks' frames, each after the frames inside it.</summary>
    public List<PlacedBlock> Blocks { get; } = [];

    /// <summary>How many calls and answers a numbered sequence has counted so far.</summary>
    private int _number;

    /// <summary>
    /// Lays steps from a height down, in a room from its left: the foot of the last one, or the height it started
    /// at when there are none.
    /// </summary>
    public double Walk(IReadOnlyList<SequenceStep> steps, double left, double y, double room, int depth)
    {
        double foot = y;
        foreach (var step in steps)
        {
            foot = step is BlockStep block
                ? Block(block, left, y, room, depth)
                : Leaf(step, left, y, room);
            y = foot + measures.StepGap;
        }

        return foot;
    }

    /// <summary>
    /// A block: its tab, its steps a level in, each other case under a divider with its own tab, and a frame round
    /// them all, as wide as its room. Its foot is where the frame ends.
    /// </summary>
    private double Block(BlockStep block, double left, double top, double room, int depth)
    {
        var lines = measures.LabelLines(block.Block, block.Label, room);
        double inLeft = left + measures.Inset, inRoom = room - 2 * measures.Inset;
        double foot = Walk(block.Steps, inLeft, top + measures.Frames.TabHeight(lines), inRoom, depth + 1);

        // Each other case's divider stands under the steps before it, and its steps under its own tab.
        var cases = new List<PlacedCase>();
        foreach (var @case in block.Else ?? [])
        {
            var caseLines = measures.CaseLines(@case.Label, room);
            double divider = foot + measures.BlockEnd;
            cases.Add(new PlacedCase(@case.Label, caseLines, divider));
            foot = Walk(@case.Steps, inLeft, divider + measures.Frames.TabHeight(caseLines), inRoom, depth + 1);
        }

        double bottom = foot + measures.BlockEnd;
        Blocks.Add(new PlacedBlock(block, lines, cases, depth, geometry.Box(left, top, room, bottom - top)));
        return bottom;
    }

    /// <summary>
    /// A call, an answer or a note, numbered when the sequence counts them: its ends on one line when they fit
    /// beside its number, and its words wrapped to what the bar leaves. Its foot is where its box ends.
    /// </summary>
    private double Leaf(SequenceStep step, double left, double y, double room)
    {
        if (step is NoteStep note)
        {
            var noteLines = measures.NoteLines(note.Text, room);
            var glass = geometry.Box(left, y, measures.NoteWidth(noteLines, room), measures.NoteHeight(noteLines));
            Steps.Add(new PlacedStep(step, null, noteLines, glass));
            return glass.Bottom;
        }

        int? number = numbered ? ++_number : null;
        var (from, to, text) = Ends(step);
        var (sender, receiver) = (people[from], people[to]);

        // The ends share their line with the number's ring, when there is one.
        double words = room - measures.Rail;
        double endsRoom = number is null ? words : words - measures.Ring;
        bool wrapped = from != to && measures.EndsWidth(sender, receiver) > endsRoom;
        var lines = measures.Lines(text, words);
        var box = geometry.Box(left, y, room, measures.StepHeight(wrapped, lines));
        Steps.Add(new PlacedStep(step, new PlacedEnds(sender, receiver, wrapped), lines, box, number));
        return box.Bottom;
    }

    /// <summary>A call's or an answer's sender, receiver and words.</summary>
    /// <remarks>A switch on the step's kind, one arm a kind (ADR 0051).</remarks>
    private static (string From, string To, string Text) Ends(SequenceStep step) => step switch
    {
        MessageStep message => (message.From, message.To, message.Text),
        ReplyStep reply => (reply.From, reply.To, reply.Text),
        _ => throw new NotSupportedException(step.GetType().Name)
    };
}
