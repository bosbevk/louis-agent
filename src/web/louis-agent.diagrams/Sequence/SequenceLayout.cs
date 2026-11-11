using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Sequence;

/// <summary>
/// Lays a sequence out: the heads in a row, each lifeline as far apart from the others as the words between them
/// need, and the steps under them, row by row, each block framed round its own (<see cref="SequenceRows"/>).
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

    /// <summary>Clear room between a message's words and either lifeline.</summary>
    private const double Clear = 12;

    /// <summary>How far the lifelines run under the last row.</summary>
    private const double LifelineEnd = 16;

    /// <summary>Where every head, every step and every block's frame stands.</summary>
    public static PlacedSequence Place(SequenceSpec spec)
    {
        var lines = spec.Participants.Select(SequenceText.HeadLines).ToList();
        var widths = spec.Participants.Select((head, at) => SequenceText.HeadWidth(head, lines[at])).ToList();
        var index = spec.Participants.Select((head, at) => (head.Id, at)).ToDictionary(StringComparer.Ordinal);
        var centers = Centers(spec, widths, index);
        double headHeight = lines.Max(SequenceText.HeadHeight);

        // A note or a frame may reach left of the first head: then every lifeline moves right and the rows go again.
        var rows = new SequenceRows(centers, index, spec.Numbered);
        var (bottom, left, right) = rows.Walk(spec.Steps, headHeight + RowGap, 0);
        if (left < 0)
        {
            centers = centers.Select(center => center - left).ToArray();
            rows = new SequenceRows(centers, index, spec.Numbered);
            (bottom, _, right) = rows.Walk(spec.Steps, headHeight + RowGap, 0);
        }

        var heads = spec.Participants
            .Select((head, at) =>
                new PlacedHead(head, lines[at], centers[at] - widths[at] / 2, 0, widths[at], headHeight))
            .ToList();
        double width = Math.Max(right, heads[^1].X + heads[^1].W);
        return new PlacedSequence(heads, rows.Rows, rows.Blocks, width, bottom + LifelineEnd);
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
        foreach (var step in spec.Steps.SelectMany(step => step.All()).Where(step => step is MessageStep or ReplyStep))
        {
            var ends = step.Names().Select(id => index[id]).ToList();
            double words = SequenceText.WordsWidth(SequenceText.Lines(SequenceRows.Text(step)));
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
}
