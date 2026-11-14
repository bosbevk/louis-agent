using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Flow.Routing;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Flow;

/// <summary>
/// Which column each box stands in when the diagram doesn't say: one past the furthest box that points at it, so a
/// chart read from the docs needs no column written (CL60.2). A column the diagram gives is kept.
/// </summary>
/// <remarks>
/// Longest-path layering, the first step of layered drawing (Sugiyama): a depth-first walk from the boxes nothing
/// points at gives an order in which every box comes after the boxes pointing at it, and an arrow that points back at
/// a box still being walked closes a cycle, so it is left out of the ranking and the walk ends. Each box then stands
/// one past the furthest of the boxes before it.
/// </remarks>
public static class Ranks
{
    /// <summary>What starts a waypoint's id: no id in a diagram's file can, since the reader takes letters.</summary>
    private const string Waypoint = "→";

    /// <summary>Whether a box is a waypoint, which the drawing never shows.</summary>
    public static bool IsWaypoint(string id) => id.StartsWith(Waypoint, StringComparison.Ordinal);

    /// <summary>
    /// The diagram with a waypoint in every column an arrow passes between its ends (CL60.2), so the layout orders it
    /// beside that column's boxes and the arrow runs through the gaps, never across a box. Each columns must be set.
    /// </summary>
    /// <remarks>
    /// Dummy nodes, the second step of layered drawing (Sugiyama): a long edge becomes a chain of short ones through
    /// boxes nobody sees, so every arrow joins neighbouring columns and the ordering keeps it clear of the rest. The
    /// word rides on the first link; <see cref="Joined"/> makes the chain one arrow again.
    /// </remarks>
    public static GraphSpec Through(GraphSpec spec)
    {
        var byId = spec.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var nodes = spec.Nodes.ToList();
        var edges = new List<GraphEdge>();
        foreach (var edge in spec.Edges)
        {
            bool known = byId.TryGetValue(edge.From, out var from) & byId.TryGetValue(edge.To, out var to);
            if (!known || to!.Column - from!.Column <= 1)
            {
                edges.Add(edge);
                continue;
            }

            // One waypoint a column between, in the group both ends share, if they do.
            string? group = from.Group == to.Group ? from.Group : null;
            string previous = edge.From;
            for (int column = from.Column!.Value + 1; column < to.Column!.Value; column++)
            {
                string id = $"{Waypoint}{edges.Count}{Waypoint}{column}";
                nodes.Add(new GraphNode(id, "", Column: column, Group: group));
                edges.Add(new GraphEdge(previous, id, previous == edge.From ? edge.Word : null, edge.Style));
                previous = id;
            }

            edges.Add(new GraphEdge(previous, edge.To, null, edge.Style));
        }

        return spec with
        {
            Nodes = nodes,
            Edges = edges
        };
    }

    /// <summary>
    /// Every waypoint moved in line with where its arrow arrives (<see cref="ApartArrows.EntryOf"/>), where no box
    /// in its column stands in the way, so the arrow runs along and then straight into its target instead of
    /// jogging to a slot and back (Louis, of the CLI and accounts charts). An arrow that can run neither there nor
    /// at the height it leaves at the whole way runs at the height it leaves at as far as it must, and in line
    /// with where it arrives from there (F134.12, Louis: "why is that line not straight"). If that can't be
    /// either, it passes the boxes in its way between two of a column or beside one, when that is a shorter way
    /// than where its waypoints stand (Louis: "why did it take a longer route?").
    /// </summary>
    /// <remarks>
    /// The alignment step of layered drawing (Brandes and Köpf straighten long edges the same way): a waypoint is
    /// drawn as nothing, so it may stand anywhere its line crosses no box. The ways an arrow could run are listed
    /// best first and the first clear one is taken (generate and test, F134.12), on three lines at most. A shortest
    /// path worked out column by column would find ways with more steps too, and no picture in the docs needs one.
    /// </remarks>
    public static List<PlacedNode> Straightened(List<PlacedNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        var byId = nodes.ToDictionary(node => node.Node.Id, StringComparer.Ordinal);
        // An arrow is placed among the others that run forward into the same box.
        var forward = edges.Where(edge => byId.ContainsKey(edge.From) && byId.ContainsKey(edge.To)
                                                                      && byId[edge.To].Node.Column >
                                                                      byId[edge.From].Node.Column).ToList();
        var moved = new Dictionary<string, IBox>(StringComparer.Ordinal);
        // Every long arrow from its real source, and the box each waypoint's arrow started at: two arrows of one
        // box may share a line, as its line out.
        var chains = edges.Where(edge => !IsWaypoint(edge.From) && IsWaypoint(edge.To))
            .Select(first => Chain(first, edges)).ToList();
        var startOf = chains.SelectMany(chain => chain.Waypoints.Select(id => (Id: id, chain.First.From)))
            .ToDictionary(waypoint => waypoint.Id, waypoint => waypoint.From, StringComparer.Ordinal);

        // Where a waypoint's middle is now, moved or not.
        double Now(string id) => moved.GetValueOrDefault(id, byId[id].Box).Middle.Y;

        // Whether another box's arrow already runs through this waypoint's column nearer than two lines may run.
        bool Taken(string id, double middle) => startOf.Keys.Any(other =>
            startOf[other] != startOf[id] && InColumnOf(byId[id], byId[other])
                                          && Math.Abs(Now(other) - middle) < ApartArrows.LineGap);

        // Whether a waypoint can stand on a line: no box of its column in the way, and no other box's arrow.
        bool Open(string id, double middle) => Free(byId[id], middle, nodes) && !Taken(id, middle);

        // Every long arrow is looked at again while one still moves (F134.12): a line another arrow stood on when
        // this one was looked at may be clear, now that arrow has moved to its own.
        bool moves = true;
        for (int pass = 0; moves && pass < chains.Count; pass++)
        {
            moves = false;
            foreach (var (first, chain, last) in chains)
            {
                var source = byId[first.From];
                double leaves = source.Box.Middle.Y, arrives = ApartArrows.EntryOf(last, forward, byId);

                // The way up and down an arrow has from where it leaves, through a line, to where it arrives.
                double Way(params double[] through) =>
                    through.Prepend(leaves).Zip(through.Append(arrives), (a, b) => Math.Abs(a - b)).Sum();
                double now = Way([.. chain.Select(Now)]);

                // Its waypoints on three lines at most: the first few on the line it leaves on, the last few on the
                // line it arrives on, and the rest on a line between. And how often such a way steps to another line.
                double[] On(int leaving, double line, int arriving) =>
                [
                    .. Enumerable.Repeat(leaves, leaving), .. Enumerable.Repeat(line, chain.Count - leaving - arriving),
                    .. Enumerable.Repeat(arrives, arriving)
                ];
                int Steps(double[] through) => through.Prepend(leaves).Zip(through.Append(arrives))
                    .Count(pair => Math.Abs(pair.First - pair.Second) > 0.01);

                // The ways it could run, the best first. One line the whole way: the one it arrives on, else the one
                // it leaves on. Then the leaving line past as few columns as it must and the arriving line from
                // there (F134.12), which is one step and no way round.
                var straight = new[] { 0, chain.Count }.Concat(Enumerable.Range(1, Math.Max(0, chain.Count - 1)))
                    .Select(leaving => On(leaving, arrives, 0));
                // Last a line past the boxes in its way, between two or beside one, when that is shorter than where
                // it stands: the shortest such way, and of two as short the one with fewer steps.
                var columns = chain.Select(id => byId[id]).ToList();
                var passes = Gaps(columns, nodes).Concat(Beside(columns, nodes));
                var round = passes.SelectMany(line => Enumerable.Range(0, chain.Count).SelectMany(leaving =>
                        Enumerable.Range(0, chain.Count - leaving).Select(arriving => On(leaving, line, arriving))))
                    .Where(option => Way(option) < now - 1).OrderBy(option => Way(option)).ThenBy(Steps);
                double[]? lines = straight.Concat(round).FirstOrDefault(option =>
                    chain.Zip(option).All(waypoint => Open(waypoint.First, waypoint.Second)));
                if (lines is null) continue;

                moves |= chain.Zip(lines).Any(waypoint => Math.Abs(Now(waypoint.First) - waypoint.Second) > 0.01);
                foreach (var (id, line) in chain.Zip(lines)) moved[id] = byId[id].Box.OnLine(line);
            }
        }

        return [.. nodes.Select(node => moved.TryGetValue(node.Node.Id, out var box) ? node with { Box = box } : node)];
    }

    /// <summary>Whether a box stands in a waypoint's column: the boxes of a column share their left edge.</summary>
    private static bool InColumnOf(PlacedNode waypoint, PlacedNode other) => other.Box.Left == waypoint.Box.Left;

    /// <summary>A long arrow as its first link, the waypoints it passes in order, and its last link.</summary>
    private static (GraphEdge First, List<string> Waypoints, GraphEdge Last) Chain(GraphEdge first,
        IReadOnlyList<GraphEdge> edges)
    {
        var waypoints = new List<string>();
        var last = first;
        while (IsWaypoint(last.To))
        {
            waypoints.Add(last.To);
            last = edges.First(edge => edge.From == waypoints[^1]);
        }

        return (first, waypoints, last);
    }

    /// <summary>Whether a line through a waypoint's column at this height passes clear of every box in it.</summary>
    private static bool Free(PlacedNode waypoint, double middle, IReadOnlyList<PlacedNode> nodes) =>
        !nodes.Any(other => !IsWaypoint(other.Node.Id) && InColumnOf(waypoint, other)
                                                       && other.Box.Top - Clear < middle
                                                       && other.Box.Bottom + Clear > middle);

    /// <summary>How far clear of a box a straightened line keeps.</summary>
    private const double Clear = 6;

    /// <summary>
    /// The lines a chain could run on between two boxes: the middle of the room between each two boxes that stand
    /// under one another in a column it passes. Only between two boxes of one group, or of none, so the line never
    /// runs along a group's frame or over its name.
    /// </summary>
    private static IEnumerable<double> Gaps(IEnumerable<PlacedNode> chain, IReadOnlyList<PlacedNode> nodes) =>
        chain.SelectMany(waypoint =>
        {
            var column = nodes.Where(other => !IsWaypoint(other.Node.Id) && InColumnOf(waypoint, other))
                .OrderBy(other => other.Box.Top).ToList();
            return column.Zip(column.Skip(1)).Where(pair => pair.First.Node.Group == pair.Second.Node.Group)
                .Select(pair => (pair.First.Box.Bottom + pair.Second.Box.Top) / 2);
        });

    /// <summary>
    /// The lines a chain could run on beside a box of a column it passes, a track's step clear of it on either
    /// side (F134.12), and no nearer than that to any other box of the column: where two boxes stand closer, the
    /// middle between them is the line. Only beside a box of no group, so the line never runs along a group's frame
    /// or over its name.
    /// </summary>
    private static IEnumerable<double> Beside(IEnumerable<PlacedNode> chain, IReadOnlyList<PlacedNode> nodes) =>
        chain.SelectMany(waypoint =>
        {
            var column = nodes.Where(other => !IsWaypoint(other.Node.Id) && InColumnOf(waypoint, other)).ToList();
            return column.Where(box => box.Node.Group is null)
                .SelectMany(box => new[] { box.Box.Top - EdgeRouter.Step, box.Box.Bottom + EdgeRouter.Step })
                .Where(line => column.All(box => line <= box.Box.Top - EdgeRouter.Step
                                                 || line >= box.Box.Bottom + EdgeRouter.Step));
        });

    /// <summary>
    /// The arrows with every chain through waypoints made one arrow again: its pieces joined straight across each
    /// waypoint, its word where the first piece put it.
    /// </summary>
    public static List<PlacedEdge> Joined(IReadOnlyList<PlacedEdge> edges)
    {
        var after = edges.Where(edge => IsWaypoint(edge.Edge.From)).ToDictionary(edge => edge.Edge.From);
        var joined = new List<PlacedEdge>();
        foreach (var first in edges.Where(edge => !IsWaypoint(edge.Edge.From)))
        {
            var edge = first;
            while (IsWaypoint(edge.Edge.To) && after.TryGetValue(edge.Edge.To, out var next))
                edge = edge with
                {
                    Edge = edge.Edge with { To = next.Edge.To },
                    Way = edge.Way.Then(next.Way),
                    Length = edge.Length + next.Length,
                    Right = Math.Max(edge.Right, next.Right)
                };
            joined.Add(edge);
        }

        return joined;
    }

    /// <summary>Every box's column, the given ones kept.</summary>
    public static IReadOnlyDictionary<string, int> Of(GraphSpec spec)
    {
        var byId = spec.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var arrows = spec.Edges.Where(edge => byId.ContainsKey(edge.From) && byId.ContainsKey(edge.To)).ToList();
        var (order, closing) = Walk(spec.Nodes, arrows);

        // In walk order every box's forward arrows in are ranked before it.
        var forward = arrows.Where(arrow => !closing.Contains(arrow)).ToList();
        var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string id in order)
            ranks[id] = byId[id].Column ?? forward.Where(arrow => arrow.To == id)
                .Select(arrow => ranks[arrow.From] + 1).DefaultIfEmpty(0).Max();
        return ranks;
    }

    /// <summary>
    /// The boxes in an order where each comes after the boxes pointing at it, and the arrows that close a cycle.
    /// </summary>
    private static (List<string> Order, HashSet<GraphEdge> Closing) Walk(
        IReadOnlyList<GraphNode> nodes,
        List<GraphEdge> arrows)
    {
        var onPath = new HashSet<string>(StringComparer.Ordinal);
        var done = new HashSet<string>(StringComparer.Ordinal);
        var finished = new List<string>();
        var closing = new HashSet<GraphEdge>();

        void Visit(string id)
        {
            onPath.Add(id);
            foreach (var arrow in arrows.Where(arrow => arrow.From == id))
            {
                // An arrow back to a box still on the path closes a cycle.
                if (onPath.Contains(arrow.To))
                {
                    closing.Add(arrow);
                    continue;
                }

                if (!done.Contains(arrow.To)) Visit(arrow.To);
            }

            onPath.Remove(id);
            done.Add(id);
            finished.Add(id);
        }

        // The boxes nothing points at first, in the diagram's order, then any a cycle left unwalked.
        var pointedAt = arrows.Select(arrow => arrow.To).ToHashSet(StringComparer.Ordinal);
        foreach (var node in nodes.OrderBy(node => pointedAt.Contains(node.Id)))
            if (!done.Contains(node.Id))
                Visit(node.Id);

        finished.Reverse();
        return (finished, closing);
    }
}
