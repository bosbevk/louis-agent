using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec;

namespace LouisAgent.Diagrams.Sequence;

/// <summary>
/// Lays a sequence out: the heads in a row, each lifeline as far apart from the others as the words between them
/// need, and a row per step under them, as tall as its words.
/// </summary>
/// <remarks>
/// A lifeline's place is a longest path over the lifelines from left to right: every message between two of them,
/// and every pair of neighbours, asks for a least distance, and each lifeline stands at the furthest of the places
/// the ones left of it ask for. A long message spreads over every gap it crosses that way, and none grows wider than
/// its words need.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data</seealso>
public static class SequenceLayout
{
    /// <summary>The least room between two heads, and between the heads and the first row.</summary>
    private const double HeadGap = 40, RowGap = 20;

    /// <summary>Clear room between a message's words and either lifeline, and under a row.</summary>
    private const double Clear = 12, RowEnd = 12;

    /// <summary>How far the lifelines run under the last row.</summary>
    private const double LifelineEnd = 16;

    /// <summary>Where every head and every step stands.</summary>
    public static PlacedSequence Place(SequenceSpec spec)
    {
        var lines = spec.Participants.Select(SequenceText.HeadLines).ToList();
        var widths = spec.Participants.Select((head, at) => SequenceText.HeadWidth(head, lines[at])).ToList();
        var index = spec.Participants.Select((head, at) => (head.Id, at)).ToDictionary(StringComparer.Ordinal);
        var centers = Centers(spec, widths, index);

        // A note wider than the lifelines it stands over may reach left of the first head: move them all right.
        double left = spec.Steps.OfType<NoteStep>().Select(note => NoteLeft(note, centers, index)).Append(0).Min();
        for (int at = 0; at < centers.Length; at++) centers[at] -= left;

        double headHeight = lines.Max(SequenceText.HeadHeight);
        var heads = spec.Participants
            .Select((head, at) =>
                new PlacedHead(head, lines[at], centers[at] - widths[at] / 2, 0, widths[at], headHeight))
            .ToList();

        // Each step takes the next row down; a numbered chart counts its arrows, never a note.
        var rows = new List<PlacedRow>();
        double y = headHeight + RowGap;
        int number = 0;
        foreach (var step in spec.Steps)
        {
            int? count = spec.Numbered && step is not NoteStep ? ++number : null;
            rows.Add(Row(step, centers, index, y) with { Number = count });
            y += rows[^1].H;
        }

        // The drawing reaches the last head, or a note or a turn to oneself's words if they stand further right.
        double width = rows.Select(Right).Append(heads[^1].X + heads[^1].W).Max();
        return new PlacedSequence(heads, rows, width, y + LifelineEnd);
    }

    /// <summary>
    /// A step's row at the given height: a note over the lifelines it names, an arrow between its two, as tall as
    /// its words, a turn to oneself a little taller.
    /// </summary>
    private static PlacedRow Row(SequenceStep step, double[] centers, IReadOnlyDictionary<string, int> index,
        double y)
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

    /// <summary>Where a note's panel would start, with the lifelines where they stand now.</summary>
    private static double NoteLeft(NoteStep note, double[] centers, IReadOnlyDictionary<string, int> index)
    {
        var xs = note.Over.Select(id => centers[index[id]]).ToList();
        return (xs.Min() + xs.Max()) / 2 -
               SequenceText.NoteWidth(SequenceText.Lines(note.Text), xs.Max() - xs.Min()) / 2;
    }

    /// <summary>How far right a row reaches past its lifelines: a note's panel, or a turn to oneself's words.</summary>
    private static double Right(PlacedRow row)
    {
        if (row.Step is NoteStep)
        {
            var note = SequenceText.NoteBox(row);
            return note.X + note.W;
        }

        if (!row.Self) return 0;
        var words = SequenceText.WordBox(row);
        return words.X + words.W + Clear;
    }

    /// <summary>
    /// Each lifeline's place: the furthest of what its neighbour and every message ending on it ask, left to right.
    /// </summary>
    private static double[] Centers(SequenceSpec spec, IReadOnlyList<double> widths,
        IReadOnlyDictionary<string, int> index)
    {
        // Each ask: lifeline b stands at least this far right of lifeline a.
        var asks = Enumerable.Range(1, widths.Count - 1)
            .Select(at => (A: at - 1, B: at, Need: widths[at - 1] / 2 + widths[at] / 2 + HeadGap))
            .ToList();
        foreach (var step in spec.Steps.Where(step => step is not NoteStep))
        {
            var ends = step.Names().Select(id => index[id]).ToList();
            double words = SequenceText.WordsWidth(SequenceText.Lines(Text(step)));
            int a = ends.Min(), b = ends.Max();
            if (a != b) asks.Add((a, b, words + 2 * Clear));
            // A turn to oneself puts its words right of its lifeline, before the next one.
            else if (a + 1 < widths.Count)
                asks.Add((a, a + 1, SequenceText.SelfOut + SequenceText.WordPad + words + Clear));
        }

        var centers = new double[widths.Count];
        centers[0] = widths[0] / 2;
        foreach (var ask in asks.OrderBy(ask => ask.B))
            centers[ask.B] = Math.Max(centers[ask.B], centers[ask.A] + ask.Need);
        return centers;
    }

    /// <summary>A step's words.</summary>
    private static string Text(SequenceStep step) =>
        step switch
        {
            MessageStep message => message.Text,
            ReplyStep reply => reply.Text,
            NoteStep note => note.Text,
            _ => throw new NotSupportedException(step.GetType().Name)
        };
}
