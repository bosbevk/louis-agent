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
