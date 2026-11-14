namespace LouisAgent.Diagrams.Flow.Rows;

/// <summary>
/// The rule a flow's boxes are placed by (F134.8): a box stands on the row level with the box that feeds it, where
/// that row is free, so the line between them runs straight (Louis: parallel means straight and level).
/// </summary>
/// <remarks>
/// One of the two strategies behind <see cref="IRowRule"/>, the one that replaces <see cref="CentredRows"/> kind by
/// kind. It is the coordinate assignment step of layered drawing, kept on ADR 0103's row grid: a box only ever
/// stands on a whole row, so two boxes are level or a row apart, and no line jogs by a few pixels.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
public sealed class LevelRows : IRowRule
{
    /// <summary>
    /// What a row away from the middle of its band costs a box, as a part of a row: so little that it only settles
    /// which of two equally good rows a box takes, and leaves a box nothing pulls in the middle.
    /// </summary>
    private const double Settle = 0.01;

    /// <summary>
    /// The bands that start at the top stand beside each other, and their rows line up, so a box in one can be level
    /// with a box in the other. They share one row height. But one whose tallest box is far taller than the rest
    /// gets rows that span three of the shared ones, or five: an odd number, so the middle of its row is still the
    /// middle of a shared row, and one tall box doesn't make every row of the band beside it as tall as itself. A
    /// band under another starts wherever that one ends, so its rows can't line up with anyone's, and it keeps its
    /// own row height.
    /// </summary>
    public IReadOnlyList<double> RowHeights(IReadOnlyList<BandShape> bands, double gap)
    {
        // A band starts at the top when it shares a column with no band before it.
        var top = bands.Where((band, at) => !bands.Take(at).Any(before => before.Columns.Overlaps(band.Columns)))
            .ToHashSet();
        // They share the row height, of their own, that leaves the tallest of them shortest.
        double shared = top.Select(band => band.Tallest).OrderDescending()
            .MinBy(height => top.Max(band => band.Rows * (Spanning(band.Tallest, height, gap) + gap)));
        return [.. bands.Select(band => top.Contains(band) ? Spanning(band.Tallest, shared, gap) : band.Tallest)];
    }

    /// <summary>
    /// How tall a row is that holds a box and spans a whole, odd number of shared rows with the gaps between them.
    /// </summary>
    private static double Spanning(double box, double shared, double gap)
    {
        int rows = (int)Math.Ceiling((box + gap) / (shared + gap));
        int odd = rows % 2 == 0 ? rows + 1 : rows;
        return odd * (shared + gap) - gap;
    }

    /// <summary>
    /// The columns from the left, each box on the row nearest the boxes that feed it. Then from the right, so a box
    /// nothing feeds stands level with what it feeds. Then from the left once more, for the boxes fed by one that
    /// moved.
    /// </summary>
    public IReadOnlyDictionary<string, double> RowTops(RowGrid grid)
    {
        var standing = new Standing(grid);
        var forward = Enumerable.Range(0, grid.Columns.Count).ToList();
        foreach (int column in forward) standing.Stand(column, fedOnly: true);
        foreach (int column in Enumerable.Reverse(forward)) standing.Stand(column, fedOnly: false);
        foreach (int column in forward) standing.Stand(column, fedOnly: false);
        return standing.Tops();
    }

    /// <summary>
    /// The rows a column's boxes of one band take, in the column's order: of every way to stand them on rows that
    /// rise, the one that leaves them nearest where each wants to stand.
    /// </summary>
    /// <remarks>
    /// Dynamic programming: the least cost of standing the first boxes with the last of them on a row is that box's
    /// own cost there plus the least cost of the ones before it on any row above. A few boxes on a few rows, so
    /// every way is weighed and the best one is exact, where taking each box's nearest free row in turn can push a
    /// later box far from where it wants to be.
    /// </remarks>
    private static int[] Rows(IReadOnlyList<double?> wants, int rows, Func<int, double> middle, double pitch)
    {
        int boxes = wants.Count;
        if (boxes == 0) return [];

        double Cost(int box, int row) => (wants[box] is { } want ? Math.Abs(middle(row) - want) : 0)
                                         + Settle * pitch * Math.Abs(row - ((rows - boxes) / 2.0 + box));

        // A box can only stand where the ones before it and after it still have a row each.
        IEnumerable<int> Free(int box) => Enumerable.Range(box, rows - boxes + 1);

        var least = new double[boxes][];
        var before = new int[boxes][];
        for (int box = 0; box < boxes; box++)
        {
            (least[box], before[box]) = (new double[rows], new int[rows]);
            foreach (int row in Free(box))
            {
                // The box before stands on the cheapest of its rows above this one. The first has none before it.
                int above = box == 0
                    ? -1
                    : Free(box - 1).Where(other => other < row).MinBy(other => least[box - 1][other]);
                before[box][row] = above;
                least[box][row] = Cost(box, row) + (box == 0 ? 0 : least[box - 1][above]);
            }
        }

        // Back from the last box's cheapest row, each box where the one after it found it.
        var taken = new int[boxes];
        taken[^1] = Free(boxes - 1).MinBy(row => least[^1][row]);
        for (int box = boxes - 1; box > 0; box--) taken[box - 1] = before[box][taken[box]];
        return taken;
    }

    /// <summary>
    /// Where each box stands while the columns are gone through: its band and its row, and so the height of its
    /// middle, which is what a box in the next column wants to be level with.
    /// </summary>
    private sealed class Standing(RowGrid grid)
    {
        private readonly Dictionary<string, (int Band, int Row)> _rows = new(StringComparer.Ordinal);

        private readonly Dictionary<string, int> _columnOf = grid.Columns
            .SelectMany((column, at) => column.Select(node => (node.Id, at)))
            .ToDictionary(pair => pair.Id, pair => pair.at, StringComparer.Ordinal);

        /// <summary>
        /// Stands one column's boxes, band by band. A box wants the height of the boxes that feed it. When boxes
        /// nothing feeds are placed too, such a box wants the height of the boxes it feeds.
        /// </summary>
        public void Stand(int column, bool fedOnly)
        {
            for (int band = 0; band < grid.Rows.Count; band++)
            {
                var members = grid.Columns[column].Where(node => grid.BandOf(node) == band).ToList();
                var wants = members.Select(node => Wants(node.Id, column, fedOnly)).ToList();
                int at = band;
                int[] rows = Rows(wants, grid.Rows[band], row => Middle(at, row), grid.Heights[band] + grid.Gap);
                for (int box = 0; box < members.Count; box++) _rows[members[box].Id] = (band, rows[box]);
            }
        }

        /// <summary>Where each box's row starts.</summary>
        public IReadOnlyDictionary<string, double> Tops() => _rows.ToDictionary(pair => pair.Key,
            pair => grid.Tops[pair.Value.Band] + pair.Value.Row * (grid.Heights[pair.Value.Band] + grid.Gap),
            StringComparer.Ordinal);

        /// <summary>
        /// The height a box wants its middle at: the median of the boxes that feed it, or for a box nothing feeds,
        /// of the boxes it feeds. Nothing, while none of them stands yet.
        /// </summary>
        private double? Wants(string id, int column, bool fedOnly)
        {
            var feeders = grid.Edges.Where(edge => edge.To == id && Column(edge.From) < column)
                .Select(edge => edge.From).ToList();
            var fed = grid.Edges.Where(edge => edge.From == id && Column(edge.To) > column).Select(edge => edge.To);
            var pulls = (feeders.Count > 0 || fedOnly ? feeders : fed).Where(_rows.ContainsKey)
                .Select(other => Middle(_rows[other].Band, _rows[other].Row)).Order().ToList();
            if (pulls.Count == 0) return null;

            return (pulls[(pulls.Count - 1) / 2] + pulls[pulls.Count / 2]) / 2;
        }

        /// <summary>
        /// A box's column, or none for an id the picture lacks, which is then before and after nothing.
        /// </summary>
        private int? Column(string id) => _columnOf.TryGetValue(id, out int at) ? at : null;

        /// <summary>The height of the middle of a band's row.</summary>
        private double Middle(int band, int row) =>
            grid.Tops[band] + row * (grid.Heights[band] + grid.Gap) + grid.Heights[band] / 2;
    }
}
