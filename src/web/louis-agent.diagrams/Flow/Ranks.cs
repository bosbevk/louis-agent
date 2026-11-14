using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Routing;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow;

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
    /// Every waypoint moved in line with where its arrow arrives, which the arrows' rule says, where no box in its
    /// column stands in the way, so the arrow runs along and then straight into its target instead of jogging to a
    /// slot and back (Louis, of the CLI and accounts charts). An arrow that can run neither there nor at the height
    /// it leaves at passes between two boxes of a column, when its rule lets it and that is a shorter way than where
    /// its waypoints stand.
    /// </summary>
    /// <remarks>
    /// The alignment step of layered drawing (Brandes and Köpf straighten long edges the same way): a waypoint is
    /// drawn as nothing, so it may stand anywhere its line crosses no box.
    /// </remarks>
    public static List<PlacedNode> Straightened(List<PlacedNode> nodes, IReadOnlyList<GraphEdge> edges,
        IArrowRule rule)
    {
        var byId = nodes.ToDictionary(node => node.Node.Id, StringComparer.Ordinal);
        // The rule places an arrow among the others that run forward into the same box.
        var forward = edges.Where(edge => byId.ContainsKey(edge.From) && byId.ContainsKey(edge.To)
                                                                      && byId[edge.To].Node.Column >
                                                                      byId[edge.From].Node.Column).ToList();
        var moved = new Dictionary<string, double>(StringComparer.Ordinal);
        // The box each waypoint's arrow started at: two arrows of one box may share a line, as its line out.
        var startOf = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var first in edges.Where(edge => !IsWaypoint(edge.From) && IsWaypoint(edge.To)))
            for (string at = first.To; IsWaypoint(at); at = edges.First(edge => edge.From == at).To)
                startOf[at] = first.From;

        // Where a waypoint's middle is now, moved or not.
        double Now(string id) => moved.GetValueOrDefault(id, byId[id].Y) + byId[id].H / 2;

        // Whether another box's arrow already runs through this waypoint's column nearer than the rule lets two
        // lines run. A rule that joins arrows lets them lie on one another, so then nothing is too near.
        bool Taken(string id, double middle) => startOf.Keys.Any(other =>
            startOf[other] != startOf[id] && byId[other].X == byId[id].X
                                          && Math.Abs(Now(other) - middle) < rule.LineGap);

        // Each chain from its real source: in line with where it arrives if every waypoint can be, else with its
        // source, else between two boxes.
        foreach (var first in edges.Where(edge => !IsWaypoint(edge.From) && IsWaypoint(edge.To)))
        {
            var chain = new List<string>();
            var last = first;
            while (IsWaypoint(last.To))
            {
                chain.Add(last.To);
                last = edges.First(edge => edge.From == chain[^1]);
            }

            var source = byId[first.From];
            double leaves = source.Y + source.H / 2, arrives = rule.EntryOf(last, forward, byId);
            bool Open(double middle) => chain.All(id => Free(byId[id], middle, nodes) && !Taken(id, middle));

            // The way up and down an arrow has from where it leaves, through a line, to where it arrives.
            double Way(params double[] through) =>
                through.Prepend(leaves).Zip(through.Append(arrives), (a, b) => Math.Abs(a - b)).Sum();
            double now = Way([.. chain.Select(Now)]);
            var between = rule.PassesBetween ? Gaps(chain.Select(id => byId[id]), nodes) : [];
            double line = new[] { arrives, leaves }.Where(Open)
                .Concat(between.Where(Open).OrderBy(gap => Way(gap)).Where(gap => Way(gap) < now - 1).Take(1))
                .FirstOrDefault(double.NaN);
            if (double.IsNaN(line)) continue;
            foreach (string id in chain) moved[id] = line - byId[id].H / 2;
        }

        return [.. nodes.Select(node => moved.TryGetValue(node.Node.Id, out double y) ? node with { Y = y } : node)];
    }

    /// <summary>Whether a line through a waypoint's column at this height passes clear of every box in it.</summary>
    private static bool Free(PlacedNode waypoint, double middle, IReadOnlyList<PlacedNode> nodes) =>
        !nodes.Any(other => !IsWaypoint(other.Node.Id) && other.X == waypoint.X && other.Y - Clear < middle
                            && other.Y + other.H + Clear > middle);

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
            var column = nodes.Where(other => !IsWaypoint(other.Node.Id) && other.X == waypoint.X)
                .OrderBy(other => other.Y).ToList();
            return column.Zip(column.Skip(1)).Where(pair => pair.First.Node.Group == pair.Second.Node.Group)
                .Select(pair => (pair.First.Y + pair.First.H + pair.Second.Y) / 2);
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
                    Path = $"{edge.Path} L{next.Path[1..]}",
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
