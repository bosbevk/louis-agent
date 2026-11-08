using System.Globalization;
using LouisAgent.Web.Graphs.Placed;

namespace LouisAgent.Web.Graphs;

/// <summary>
/// Gives every box and arrow of a diagram its place (F111): the columns left to right across the width, a column's
/// boxes stacked and centred in the order that keeps the arrows apart, and each arrow a curve from one box's right
/// edge to the next box's left edge.
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
    /// <summary>How far under the drawing the first arrow that goes back runs, and the step between two such arrows.</summary>
    private const double BackLane = 34, BackStep = 18;
    /// <summary>How far out of a box's right side an arrow to itself bulges.</summary>
    private const double SelfBulge = 44;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>Places a diagram across the given width.</summary>
    public static PlacedGraph Place(GraphSpec spec, int width = 880)
    {
        // Each column's boxes, ordered so the arrows between neighbouring columns don't cross; the tallest sets the height.
        var columns = Ordered(spec);
        // Columns never touch: when the width can't hold them with room between, the drawing widens and the page scales it.
        width = Math.Max(width, (int)Math.Ceiling(columns.Count * BoxWidth + Math.Max(0, columns.Count - 1) * MinColumnGap));
        double height = columns.Count == 0 ? 0 : columns.Max(ColumnHeight);

        var nodes = new List<PlacedNode>();
        for (int at = 0; at < columns.Count; at++)
        {
            double x = ColumnX(at, columns.Count, width);
            double y = (height - ColumnHeight(columns[at])) / 2;
            foreach (var node in columns[at])
            {
                double h = HeightOf(node);
                nodes.Add(new PlacedNode(node, x, y, BoxWidth, h));
                y += h + Gap;
            }
        }

        // An arrow only between two boxes that exist. One that goes back (to an earlier column) loops under the drawing,
        // one lane each; one to its own box loops out of the box's right side.
        var byId = nodes.ToDictionary(placed => placed.Node.Id);
        var known = spec.Edges.Where(edge => byId.ContainsKey(edge.From) && byId.ContainsKey(edge.To)).ToList();
        var back = known.Where(edge => byId[edge.To].Node.Column < byId[edge.From].Node.Column).ToList();
        var edges = known.Select(edge =>
            edge.From == edge.To ? SelfLoop(edge, byId[edge.From])
            : back.Contains(edge) ? Back(edge, byId[edge.From], byId[edge.To], height + BackLane + BackStep * back.IndexOf(edge))
            : Curve(edge, byId[edge.From], byId[edge.To])).ToList();
        if (back.Count > 0) height += BackLane + BackStep * (back.Count - 1) + 8;
        if (known.Any(edge => edge.From == edge.To && byId[edge.From].Node.Column == columns.Count - 1)) width += (int)SelfBulge;

        return new PlacedGraph(width, (int)Math.Ceiling(height), nodes, edges);
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

    private static double ColumnHeight(IEnumerable<GraphNode> column)
    {
        var boxes = column.ToList();
        return boxes.Sum(HeightOf) + Gap * (boxes.Count - 1);
    }

    /// <summary>The first column at the left edge, the last at the right, the rest spread evenly; one alone in the middle.</summary>
    private static double ColumnX(int at, int count, int width) =>
        count == 1 ? (width - BoxWidth) / 2 : at * (width - BoxWidth) / (count - 1);

    /// <summary>
    /// A cubic curve that leaves the left box's right edge at its middle and arrives flat at the right box's left edge.
    /// Its length is measured along a few straight pieces, close enough for the stroke that draws it.
    /// </summary>
    private static PlacedEdge Curve(GraphEdge edge, PlacedNode from, PlacedNode to)
    {
        double x1 = from.X + from.W, y1 = from.Y + from.H / 2;
        double x2 = to.X, y2 = to.Y + to.H / 2;
        double bend = (x2 - x1) / 2;
        string path = $"M {N(x1)} {N(y1)} C {N(x1 + bend)} {N(y1)}, {N(x2 - bend)} {N(y2)}, {N(x2)} {N(y2)}";
        // Where a word sits: the curve's middle, the empty ground between two columns, so two words leaving one box part.
        var (wordX, wordY) = At(0.5, x1, y1, x1 + bend, y1, x2 - bend, y2, x2, y2);
        return new PlacedEdge(edge, path, Length(x1, y1, x1 + bend, y1, x2 - bend, y2, x2, y2), wordX, wordY);
    }

    /// <summary>
    /// An arrow back to an earlier column: down from the box's bottom middle, along a lane under the drawing, up into
    /// the target's bottom middle; its word sits on the lane.
    /// </summary>
    private static PlacedEdge Back(GraphEdge edge, PlacedNode from, PlacedNode to, double lane)
    {
        double x1 = from.X + from.W / 2, y1 = from.Y + from.H, x2 = to.X + to.W / 2, y2 = to.Y + to.H;
        string path = $"M {N(x1)} {N(y1)} C {N(x1)} {N(lane)}, {N(x2)} {N(lane)}, {N(x2)} {N(y2)}";
        var (wordX, wordY) = At(0.5, x1, y1, x1, lane, x2, lane, x2, y2);
        return new PlacedEdge(edge, path, Length(x1, y1, x1, lane, x2, lane, x2, y2), wordX, wordY + 4);
    }

    /// <summary>An arrow from a box to itself: out of its right side near the top, round, and back in near the bottom.</summary>
    private static PlacedEdge SelfLoop(GraphEdge edge, PlacedNode box)
    {
        double x = box.X + box.W, y1 = box.Y + box.H * 0.3, y2 = box.Y + box.H * 0.7, bulge = x + SelfBulge;
        string path = $"M {N(x)} {N(y1)} C {N(bulge)} {N(box.Y - 10)}, {N(bulge)} {N(box.Y + box.H + 10)}, {N(x)} {N(y2)}";
        var (wordX, wordY) = At(0.5, x, y1, bulge, box.Y - 10, bulge, box.Y + box.H + 10, x, y2);
        return new PlacedEdge(edge, path, Length(x, y1, bulge, box.Y - 10, bulge, box.Y + box.H + 10, x, y2), wordX + 6, wordY + 3);
    }

    private static string N(double value) => value.ToString("0.#", Invariant);

    /// <summary>The point on the cubic at <paramref name="t"/>, from 0 at its start to 1 at its end.</summary>
    private static (double X, double Y) At(double t, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
    {
        double u = 1 - t;
        return (u * u * u * x0 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x3,
            u * u * u * y0 + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * y3);
    }

    /// <summary>The curve's length as the sum of sixteen straight pieces along it.</summary>
    private static double Length(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
    {
        const int pieces = 16;
        double length = 0, lastX = x0, lastY = y0;
        for (int piece = 1; piece <= pieces; piece++)
        {
            var (x, y) = At((double)piece / pieces, x0, y0, x1, y1, x2, y2, x3, y3);
            length += Math.Sqrt((x - lastX) * (x - lastX) + (y - lastY) * (y - lastY));
            (lastX, lastY) = (x, y);
        }

        return length;
    }
}
