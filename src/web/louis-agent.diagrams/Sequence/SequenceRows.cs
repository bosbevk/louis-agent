using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Sequence;

/// <summary>
/// Walks a sequence's steps down the page with the lifelines where they stand: a row for each message, reply and
/// note, a frame round each block's rows, and how far left and right it all reaches.
/// </summary>
/// <remarks>
/// It walks the Composite the steps make: a block lays its own steps one level deeper, then frames what came back,
/// so a nested frame always sits inside its parent and the numbers run on in the order the steps happen. It holds
/// the rows, the frames and the count while it walks, so it is a class rather than a set of functions.
/// </remarks>
internal sealed class SequenceRows(
    IReadOnlyList<double> centers,
    IReadOnlyDictionary<string, int> index,
    bool numbered)
{
    /// <summary>Clear room between a turn to oneself's words and the drawing's edge, and under a row.</summary>
    private const double Clear = 12, RowEnd = 12;

    /// <summary>The messages, replies and notes, top to bottom.</summary>
    public List<PlacedRow> Rows { get; } = [];

    /// <summary>The blocks' frames, each after the frames inside it.</summary>
    public List<PlacedBlock> Blocks { get; } = [];

    /// <summary>How many arrows a numbered chart has counted so far.</summary>
    private int _number;

    /// <summary>Lays steps from the given height down: the height under them, and how far left and right they reach.</summary>
    public (double Y, double Left, double Right) Walk(IReadOnlyList<SequenceStep> steps, double y, int depth)
    {
        double left = double.MaxValue, right = double.MinValue;
        foreach (var step in steps)
        {
            var (bottom, from, to) = step is BlockStep block ? Block(block, y, depth) : Leaf(step, y);
            (y, left, right) = (bottom, Math.Min(left, from), Math.Max(right, to));
        }

        return (y, left, right);
    }

    /// <summary>A step's words.</summary>
    public static string Text(SequenceStep step) =>
        step switch
        {
            MessageStep message => message.Text,
            ReplyStep reply => reply.Text,
            NoteStep note => note.Text,
            BlockStep block => block.Label,
            _ => throw new NotSupportedException(step.GetType().Name)
        };

    /// <summary>A message, a reply or a note: its row, numbered when the chart counts its arrows.</summary>
    private (double Y, double Left, double Right) Leaf(SequenceStep step, double y)
    {
        int? count = numbered && step is not NoteStep ? ++_number : null;
        var row = Row(step, y) with { Number = count };
        Rows.Add(row);
        var (left, right) = Reach(row);
        return (y + row.H, left, right);
    }

    /// <summary>
    /// A block: its tab, its steps a level deeper, each other case under a divider, and a frame round them all, wide
    /// enough for its tab and its cases' labels.
    /// </summary>
    private (double Y, double Left, double Right) Block(BlockStep block, double top, int depth)
    {
        var lines = SequenceText.Lines(block.Label);
        var (y, left, right) = Walk(block.Steps, top + SequenceText.TabHeight(lines), depth + 1);

        var cases = new List<PlacedCase>();
        foreach (var @case in block.Else ?? [])
        {
            var caseLines = SequenceText.Lines(@case.Label);
            cases.Add(new PlacedCase(@case.Label, caseLines, y));
            var inner = Walk(@case.Steps, y + SequenceText.TabHeight(caseLines), depth + 1);
            (y, left, right) = (inner.Y, Math.Min(left, inner.Left), Math.Max(right, inner.Right));
        }

        double x = left - SequenceText.Inset;
        double width = cases.Select(@case => SequenceText.ElseWidth(@case.Lines))
            .Append(SequenceText.TabWidth(block.Block, lines))
            .Append(right + SequenceText.Inset - x)
            .Max();
        double bottom = y + SequenceText.BlockEnd;
        Blocks.Add(new PlacedBlock(block, lines, cases, depth, x, top, width, bottom - top));
        return (bottom, x, x + width);
    }

    /// <summary>
    /// A step's row at the given height: a note over the lifelines it names, an arrow between its two, as tall as
    /// its words, a turn to oneself a little taller.
    /// </summary>
    private PlacedRow Row(SequenceStep step, double y)
    {
        var xs = step.Names().Select(id => centers[index[id]]).ToList();
        var words = SequenceText.Lines(Text(step));
        if (step is NoteStep)
            return new PlacedRow(step, words, xs.Min(), xs.Max(), y,
                SequenceText.NoteTop + SequenceText.NoteHeight(words) + RowEnd);

        double height = SequenceText.TextTop + words.Count * SequenceText.LineStep + SequenceText.ArrowGap + RowEnd
                        + (xs[0] == xs[1] ? SequenceText.SelfDrop : 0);
        return new PlacedRow(step, words, xs[0], xs[1], y, height);
    }

    /// <summary>How far left and right a row reaches: a note's panel, a turn to oneself's words, or its arrow.</summary>
    private static (double Left, double Right) Reach(PlacedRow row)
    {
        if (row.Step is NoteStep)
        {
            var note = SequenceText.NoteBox(row);
            return (note.X, note.X + note.W);
        }

        if (!row.Self) return (Math.Min(row.FromX, row.ToX), Math.Max(row.FromX, row.ToX));
        var words = SequenceText.WordBox(row);
        return (row.FromX, words.X + words.W + Clear);
    }
}
