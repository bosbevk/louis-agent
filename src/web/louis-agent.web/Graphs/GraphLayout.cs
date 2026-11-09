using LouisAgent.Web.Graphs.Placed;

namespace LouisAgent.Web.Graphs;

/// <summary>
/// Gives every box and arrow of a diagram its place (F111): the columns left to right across the width, a column's
/// boxes on one row grid, centred, in the order that keeps the arrows apart, and each arrow a connector drawn by
/// <see cref="EdgeRouter"/>.
/// </summary>
/// <remarks>
/// A pure function over the data, so it is tested with numbers and never needs a browser. An arrow that names a box
/// the diagram doesn't have is left out, and the page's own test holds its ids. The order inside a column is the
/// barycentre heuristic of layered drawing (Sugiyama): a box goes where the mean of its neighbours in the column
/// beside it sits, swept left to right by sources and then right to left by targets, so arrows between two columns
/// don't cross (FX85, Louis: "lines may not overlap"); a box with no neighbour keeps its place.
/// </remarks>
public static class GraphLayout
{
    private const double BoxWidth = 170;
    private const double BoxHeight = 52;
    /// <summary>What a line of the note adds to a box; the note wraps at <see cref="NoteWidth"/> characters of the code font.</summary>
    private const double NoteLine = 12;
    /// <summary>How many characters of the 10px code font fit a line inside the box.</summary>
    public const int NoteWidth = 25;
    private const double Gap = 18;
    /// <summary>The least room between two columns; fewer and the drawing widens instead (FX85).</summary>
    private const double MinColumnGap = 60;

    /// <summary>Places a diagram across the given width.</summary>
    public static PlacedGraph Place(GraphSpec spec, int width = 880)
    {
        // Each column's boxes, ordered so the arrows between neighbouring columns don't cross; the tallest sets the height.
        var columns = Ordered(spec);
        // Columns never touch, and each gap holds the words that cross it, no more (Louis: "just take these empty
        // spaces out"): when the width can't hold them, the drawing widens and the page scales it.
        var gaps = Gaps(spec, columns);
        width = Math.Max(width, (int)Math.Ceiling(columns.Count * BoxWidth + gaps.Sum()));
        var lefts = Lefts(gaps, columns.Count, width);
        // Every box sits on one row grid, each row as tall as the tallest box, a shorter column centred on it (Louis:
        // "alignment is key", "this must not happen"): two middles then differ by half a row at least, never by a few
        // pixels, so no connector ever jogs. A shorter box stands in the middle of its row.
        double row = spec.Nodes.Count == 0 ? 0 : spec.Nodes.Max(HeightOf);
        double height = columns.Count == 0 ? 0 : columns.Max(column => ColumnHeight(column, row));

        var nodes = new List<PlacedNode>();
        for (int at = 0; at < columns.Count; at++)
        {
            double x = lefts[at];
            double y = (height - ColumnHeight(columns[at], row)) / 2;
            foreach (var node in columns[at])
            {
                double h = HeightOf(node);
                nodes.Add(new PlacedNode(node, x, y + (row - h) / 2, BoxWidth, h));
                y += row + Gap;
            }
        }

        // An arrow only between two boxes that exist; the router draws each as a connector and may need room outside
        // the boxes, for a lane under the drawing or a loop past the last column.
        var byId = nodes.ToDictionary(placed => placed.Node.Id);
        var known = spec.Edges.Where(edge => byId.ContainsKey(edge.From) && byId.ContainsKey(edge.To)).ToList();
        var (edges, fullHeight, fullWidth) = EdgeRouter.Route(known, byId, height, width);

        return new PlacedGraph((int)Math.Ceiling(fullWidth), (int)Math.Ceiling(fullHeight), nodes, edges);
    }

    /// <summary>The columns, each box placed by the mean position of its neighbours: by its sources, then by its targets.</summary>
    private static List<List<GraphNode>> Ordered(GraphSpec spec)
    {
        var columns = spec.Nodes.GroupBy(node => node.Column).OrderBy(column => column.Key)
            .Select(column => column.ToList()).ToList();
        for (int at = 1; at < columns.Count; at++)
            columns[at] = ByNeighbours(columns[at], columns[at - 1], spec.Edges, edge => edge.To, edge => edge.From);
        for (int at = columns.Count - 2; at >= 0; at--)
            columns[at] = ByNeighbours(columns[at], columns[at + 1], spec.Edges, edge => edge.From, edge => edge.To);
        return columns;
    }

    /// <summary>
    /// A column sorted by the mean index, in the column beside it, of the boxes each one is joined to; a box joined to
    /// none keeps its own index, and a tie keeps the data's order.
    /// </summary>
    private static List<GraphNode> ByNeighbours(List<GraphNode> column, List<GraphNode> beside,
        IReadOnlyList<GraphEdge> edges, Func<GraphEdge, string> mine, Func<GraphEdge, string> theirs)
    {
        var place = beside.Select((node, index) => (node.Id, index)).ToDictionary(pair => pair.Id, pair => (double)pair.index);
        double Key(GraphNode node, int own)
        {
            var joined = edges.Where(edge => mine(edge) == node.Id && place.ContainsKey(theirs(edge)))
                .Select(edge => place[theirs(edge)]).ToList();
            return joined.Count == 0 ? own : joined.Average();
        }

        var sorted = column.Select((node, index) => (node, key: Key(node, index))).OrderBy(pair => pair.key)
            .Select(pair => pair.node).ToList();
        // The data's order stands unless the sort takes crossings away: a column already clear keeps the order it was given.
        return Crossings(sorted, beside, edges, mine, theirs) < Crossings(column, beside, edges, mine, theirs) ? sorted : column;
    }

    /// <summary>How many pairs of arrows between the two columns cross each other.</summary>
    private static int Crossings(List<GraphNode> column, List<GraphNode> beside,
        IReadOnlyList<GraphEdge> edges, Func<GraphEdge, string> mine, Func<GraphEdge, string> theirs)
    {
        var here = column.Select((node, index) => (node.Id, index)).ToDictionary(pair => pair.Id, pair => pair.index);
        var there = beside.Select((node, index) => (node.Id, index)).ToDictionary(pair => pair.Id, pair => pair.index);
        var arrows = edges.Where(edge => here.ContainsKey(mine(edge)) && there.ContainsKey(theirs(edge)))
            .Select(edge => (From: here[mine(edge)], To: there[theirs(edge)])).ToList();
        int crossings = 0;
        for (int a = 0; a < arrows.Count; a++)
            for (int b = a + 1; b < arrows.Count; b++)
                if ((arrows[a].From - arrows[b].From) * (arrows[a].To - arrows[b].To) < 0) crossings++;
        return crossings;
    }

    private static double HeightOf(GraphNode node) => node.Note is null ? BoxHeight : BoxHeight + NoteLine * NoteLines(node.Note).Count;

    /// <summary>
    /// The note as the lines the box shows (FX85, Louis: "need wrapping"): words taken in order while a line holds
    /// <see cref="NoteWidth"/> characters, a word too long for any line on one of its own.
    /// </summary>
    public static IReadOnlyList<string> NoteLines(string note)
    {
        var lines = new List<string>();
        var line = "";
        foreach (string word in note.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > NoteWidth)
            {
                lines.Add(line);
                line = word;
                continue;
            }

            line = line.Length == 0 ? word : $"{line} {word}";
        }

        if (line.Length > 0) lines.Add(line);
        return lines;
    }

    /// <summary>A column's height on the grid: a row per box, a gap between rows.</summary>
    private static double ColumnHeight(IEnumerable<GraphNode> column, double row)
    {
        int boxes = column.Count();
        return boxes * row + Gap * (boxes - 1);
    }

    /// <summary>
    /// The room between each pair of neighbouring columns: the rooms either side of the bend for its own arrows'
    /// words (<see cref="EdgeRouter.RoomsFor"/>), never less than <see cref="MinColumnGap"/>. An arrow that skips a
    /// column bends in the middle, so it needs twice its word in every gap it crosses.
    /// </summary>
    private static List<double> Gaps(GraphSpec spec, List<List<GraphNode>> columns)
    {
        var columnOf = columns.SelectMany((column, at) => column.Select(node => (node.Id, at)))
            .ToDictionary(pair => pair.Id, pair => pair.at);
        var forward = spec.Edges.Where(edge => columnOf.ContainsKey(edge.From) && columnOf.ContainsKey(edge.To)
            && columnOf[edge.To] > columnOf[edge.From]).ToList();

        double Gap(int gap)
        {
            var neighbours = forward.Where(edge => columnOf[edge.From] == gap && columnOf[edge.To] == gap + 1).ToList();
            var (source, target) = EdgeRouter.RoomsFor(neighbours, forward);
            var skipping = forward.Where(edge => columnOf[edge.From] <= gap && columnOf[edge.To] > gap + 1
                || columnOf[edge.From] < gap && columnOf[edge.To] > gap).ToList();
            return Math.Max(MinColumnGap, Math.Max(source + target, EdgeRouter.GapFor(skipping)));
        }

        return Enumerable.Range(0, Math.Max(0, columns.Count - 1)).Select(Gap).ToList();
    }

    /// <summary>
    /// Where each column starts: the first at the left edge and the last at the right, each gap its own room plus an
    /// even share of what the width has to spare; one column alone stands in the middle.
    /// </summary>
    private static List<double> Lefts(List<double> gaps, int count, int width)
    {
        if (count == 1) return [(width - BoxWidth) / 2];

        double spare = gaps.Count == 0 ? 0 : (width - count * BoxWidth - gaps.Sum()) / gaps.Count;
        var lefts = new List<double>();
        double x = 0;
        for (int at = 0; at < count; at++)
        {
            lefts.Add(x);
            if (at < gaps.Count) x += BoxWidth + gaps[at] + spare;
        }

        return lefts;
    }
}
