using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Routing;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow;

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

    /// <summary>
    /// What a line of the note adds to a box; the note wraps at <see cref="NoteWidth"/> characters of the code font.
    /// </summary>
    private const double NoteLine = 12;

    /// <summary>How many characters of the 10px code font fit a line inside the box.</summary>
    public const int NoteWidth = 25;

    /// <summary>How wide a character of the label's 600 12px code font is, and the room beside a line.</summary>
    public const double LabelCharWidth = 7.6, LabelPad = 14;

    private const double Gap = 18;

    /// <summary>The least room between two columns; fewer and the drawing widens instead (FX85).</summary>
    private const double MinColumnGap = 60;

    /// <summary>How many rounds of sweeps, left to right and back, order the columns.</summary>
    private const int Sweeps = 4;

    /// <summary>
    /// How far from its turn an arrow's corner starts in the docs' pictures (CL60.2, Louis's pick "Both looks"): wide
    /// enough that a jog reads as a soft S. The site keeps <see cref="EdgeRouter.Bend"/>.
    /// </summary>
    public const double SoftBend = 24;

    /// <summary>Places a diagram across the given width, each arrow's corners rounded by the bend asked for.</summary>
    public static PlacedGraph Place(GraphSpec spec, int width = 880, double bend = EdgeRouter.Bend)
    {
        // A box with no column takes the one its arrows give it, so everything below sees a column on every box.
        var ranks = Ranks.Of(spec);
        spec = spec with { Nodes = [.. spec.Nodes.Select(node => node with { Column = ranks[node.Id] })] };
        // An arrow past a column runs through a waypoint in it, laid out like a box and drawn as nothing.
        spec = Ranks.Through(spec);

        // A chart that runs down is laid out to the right with every box turned, at its own size, then turned back.
        return spec.Direction == FlowDirection.Down
            ? Turn.Back(PlaceRight(spec, 0, turned: true, bend))
            : PlaceRight(spec, width, turned: false, bend);
    }

    /// <summary>
    /// The diagram laid out to the right across the given width; turned, every box's width and height swap places.
    /// </summary>
    private static PlacedGraph PlaceRight(GraphSpec spec, int width, bool turned, double bend)
    {
        Func<GraphNode, double> wide = turned ? HeightOf : WidthOf, tall = turned ? WidthOf : HeightOf;

        // Each column's boxes, ordered so the arrows between neighbouring columns don't cross; the tallest sets the
        // height.
        var columns = Ordered(spec);
        // Columns never touch, and each gap holds the words that cross it, no more (Louis: "just take these empty
        // spaces out"): when the width can't hold them, the drawing widens and the page scales it.
        var gaps = Gaps(spec, columns, turned);
        // A column is as wide as its widest box: a label's longest line, never less than the site's box.
        var widths = columns.Select(column => column.Max(wide)).ToList();
        width = Math.Max(width, (int)Math.Ceiling(widths.Sum() + gaps.Sum()));
        var lefts = Lefts(gaps, widths, width);
        // Every box sits on one row grid, each row as tall as the tallest box of its band, a shorter column centred on
        // it (Louis: "alignment is key", "this must not happen"): two middles then differ by half a row at least, never
        // by a few pixels, so no connector ever jogs. A shorter box stands in the middle of its row.
        var (banded, height) = Bands.Place(spec, columns, new Bands.Grid(lefts, widths, Gap, tall), turned);
        // A long arrow's waypoints stand in line with its target, so it drops straight in.
        var placed = Ranks.Straightened(banded, spec.Edges);
        // A group's frame goes round its boxes; the drawing moves and grows so every frame is inside it.
        var (nodes, groups, fitWidth, fitHeight) = Bands.Fit(placed, Bands.Frames(spec, placed, turned), width, height);

        // An arrow only between two boxes that exist; the router draws each as a connector and may need room outside
        // the boxes, for a lane under the drawing or a loop past the last column.
        var byId = nodes.ToDictionary(node => node.Node.Id);
        var known = spec.Edges.Where(edge => byId.ContainsKey(edge.From) && byId.ContainsKey(edge.To)).ToList();
        var (edges, fullHeight, fullWidth) = EdgeRouter.Route(known, byId, fitHeight, fitWidth, turned, bend);

        // The waypoints go, and each chain through them is one arrow again.
        return new PlacedGraph((int)Math.Ceiling(fullWidth), (int)Math.Ceiling(fullHeight),
            [.. nodes.Where(node => !Ranks.IsWaypoint(node.Node.Id))], Ranks.Joined(edges), groups);
    }

    /// <summary>
    /// The columns, each box placed by the mean position of its neighbours: by its sources, then by its targets.
    /// </summary>
    private static List<List<GraphNode>> Ordered(GraphSpec spec)
    {
        var columns = spec.Nodes.GroupBy(node => node.Column).OrderBy(column => column.Key)
            .Select(column => column.ToList()).ToList();
        // A few rounds, as layered drawing sweeps: with waypoints in the columns (CL60.2) one round seldom settles a
        // docs chart, and a round that takes no crossing away changes nothing.
        for (int round = 0; round < Sweeps; round++)
        {
            for (int at = 1; at < columns.Count; at++)
                columns[at] = ByNeighbours(columns[at], columns[at - 1], spec.Edges, edge => edge.To,
                    edge => edge.From);
            for (int at = columns.Count - 2; at >= 0; at--)
                columns[at] = ByNeighbours(columns[at], columns[at + 1], spec.Edges, edge => edge.From,
                    edge => edge.To);
        }

        return columns;
    }

    /// <summary>
    /// A column sorted by the mean index, in the column beside it, of the boxes each one is joined to; a box joined to
    /// none keeps its own index, and a tie keeps the data's order.
    /// </summary>
    private static List<GraphNode> ByNeighbours(List<GraphNode> column, List<GraphNode> beside,
        IReadOnlyList<GraphEdge> edges, Func<GraphEdge, string> mine, Func<GraphEdge, string> theirs)
    {
        var place = beside.Select((node, index) => (node.Id, index))
            .ToDictionary(pair => pair.Id, pair => (double)pair.index);

        double Key(GraphNode node, int own)
        {
            var joined = edges.Where(edge => mine(edge) == node.Id && place.ContainsKey(theirs(edge)))
                .Select(edge => place[theirs(edge)]).ToList();
            return joined.Count == 0 ? own : joined.Average();
        }

        var sorted = column.Select((node, index) => (node, key: Key(node, index))).OrderBy(pair => pair.key)
            .Select(pair => pair.node).ToList();
        // The data's order stands unless the sort takes crossings away: a column already clear keeps the order it was
        // given.
        return Crossings(sorted, beside, edges, mine, theirs) < Crossings(column, beside, edges, mine, theirs)
            ? sorted
            : column;
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
                if ((arrows[a].From - arrows[b].From) * (arrows[a].To - arrows[b].To) < 0)
                    crossings++;
        return crossings;
    }

    /// <summary>A box's height: its head, and under it its members, each a line (CL60.4).</summary>
    private static double HeightOf(GraphNode node) =>
        HeadOf(node) + (node.Members is { Count: > 0 }
            ? FlowText.MemberTop + FlowText.MemberLines(node).Count * FlowText.MemberStep + FlowText.MemberEnd
            : 0);

    /// <summary>
    /// A box's head, its label and note: a line more for each label line after the first, and the note's lines.
    /// </summary>
    public static double HeadOf(GraphNode node)
    {
        double label = BoxHeight + FlowText.LabelStep * (FlowText.LabelLines(node).Count - 1);
        return node.Note is null ? label : label + NoteLine * NoteLines(node.Note).Count;
    }

    /// <summary>
    /// A box's width: its label's longest line and the room beside it, and its icon's badge when it has one, or its
    /// longest member if that is wider, at least the site's box.
    /// </summary>
    private static double WidthOf(GraphNode node)
    {
        double label = FlowText.LabelLines(node).Max(line => line.Length) * LabelCharWidth + 2 * LabelPad
                       + (node.Icon is null ? 0 : FlowText.IconRoom);
        double members = FlowText.MemberLines(node).Select(line => line.Length).DefaultIfEmpty(0).Max()
                         * FlowText.WordChar + 2 * LabelPad;
        return Math.Max(BoxWidth, Math.Max(label, members));
    }

    /// <summary>
    /// The note as the lines the box shows (FX85, Louis: "need wrapping"), wrapped at <see cref="NoteWidth"/>
    /// characters as every diagram's words are (<see cref="TextWrap"/>).
    /// </summary>
    public static IReadOnlyList<string> NoteLines(string note) => TextWrap.Lines(note, NoteWidth);

    /// <summary>
    /// The room between each pair of neighbouring columns: the rooms either side of the bend for its own arrows'
    /// words (<see cref="EdgeRouter.RoomsFor"/>), never less than <see cref="MinColumnGap"/>. An arrow that skips a
    /// column bends in the middle, so it needs twice its word in every gap it crosses. Laid out turned, a word stands
    /// beside its arrow once the drawing is turned back, so its room is its lines' height, not its length (CL60.2).
    /// </summary>
    private static List<double> Gaps(GraphSpec spec, List<List<GraphNode>> columns, bool turned)
    {
        var groupOf = spec.Nodes.ToDictionary(node => node.Id, node => node.Group);
        var columnOf = columns.SelectMany((column, at) => column.Select(node => (node.Id, at)))
            .ToDictionary(pair => pair.Id, pair => pair.at);
        var forward = spec.Edges.Where(edge => columnOf.ContainsKey(edge.From) && columnOf.ContainsKey(edge.To)
                                                                               && columnOf[edge.To] >
                                                                               columnOf[edge.From]).ToList();

        double Gap(int gap)
        {
            var neighbours = forward.Where(edge => columnOf[edge.From] == gap && columnOf[edge.To] == gap + 1).ToList();
            var (source, target) = EdgeRouter.RoomsFor(neighbours, forward, turned);
            var (frameOut, frameIn) = EdgeRouter.FrameRooms(neighbours, id => groupOf[id], turned);
            var skipping = forward.Where(edge => columnOf[edge.From] <= gap && columnOf[edge.To] > gap + 1
                                                 || columnOf[edge.From] < gap && columnOf[edge.To] > gap).ToList();
            return Math.Max(MinColumnGap, Math.Max(source + target + frameOut + frameIn
                                                   + EdgeRouter.TracksRoom(neighbours),
                EdgeRouter.GapFor(skipping, turned)));
        }

        return Enumerable.Range(0, Math.Max(0, columns.Count - 1)).Select(Gap).ToList();
    }

    /// <summary>
    /// Where each column starts: the first at the left edge and the last at the right, each gap its own room plus an
    /// even share of what the width has to spare; one column alone stands in the middle.
    /// </summary>
    private static List<double> Lefts(List<double> gaps, List<double> widths, int width)
    {
        if (widths.Count == 1) return [(width - widths[0]) / 2];

        double spare = gaps.Count == 0 ? 0 : (width - widths.Sum() - gaps.Sum()) / gaps.Count;
        var lefts = new List<double>();
        double x = 0;
        for (int at = 0; at < widths.Count; at++)
        {
            lefts.Add(x);
            if (at < gaps.Count) x += widths[at] + gaps[at] + spare;
        }

        return lefts;
    }
}
