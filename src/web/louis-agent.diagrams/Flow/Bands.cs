using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow;

/// <summary>
/// Stands each column's boxes on the row grid, a group's boxes in a band of rows of their own (CL60.2), and draws a
/// labelled frame around each group. A diagram without groups is one band: its columns centred, as the site's are.
/// </summary>
/// <remarks>
/// Swimlanes: every group owns the same band of rows in every column, so its frame, the box around its members, can
/// hold no box of another group, which a frame drawn round boxes the plain layout scattered could not promise. The
/// label goes on the frame's top; laid out turned, that is the band's left, so the room for it goes there.
/// </remarks>
internal static class Bands
{
    /// <summary>The room inside a frame around its boxes, the room for its label, and between two bands.</summary>
    public const double Pad = 12, LabelRoom = 22, Between = 2 * Pad + LabelRoom + 8;

    /// <summary>Every box placed on the grid, band by band, and the drawing's height.</summary>
    public static (List<PlacedNode> Nodes, double Height) Place(
        GraphSpec spec,
        List<List<GraphNode>> columns,
        Grid grid,
        bool turned)
    {
        bool grouped = spec.Groups is { Count: > 0 };
        string? Key(GraphNode node) => grouped ? node.Group : null;
        double Tall(int boxes) => boxes * grid.Row + grid.Gap * (boxes - 1);

        // Each band as tall as its fullest column, the bands one under another with room for their frames.
        var keys = spec.Nodes.Select(Key).Distinct().ToList();
        var rows = keys.Select(key => columns.Max(column => column.Count(node => Key(node) == key))).ToList();
        var tops = new List<double>();
        double y = (grouped, turned) switch
        {
            (false, _) => 0,
            (true, false) => Pad + LabelRoom,
            (true, true) => Pad
        };
        for (int band = 0; band < keys.Count; band++)
        {
            tops.Add(y);
            y += Tall(rows[band]) + (band < keys.Count - 1 ? Between : 0);
        }

        // In each column a band's boxes stand centred in their band, a box in the middle of its row.
        var nodes = new List<PlacedNode>();
        for (int at = 0; at < columns.Count; at++)
            for (int band = 0; band < keys.Count; band++)
            {
                var members = columns[at].Where(node => Key(node) == keys[band]).ToList();
                double top = tops[band] + (Tall(rows[band]) - Tall(members.Count)) / 2;
                foreach (var node in members)
                {
                    double h = grid.Tall(node);
                    nodes.Add(new PlacedNode(node, grid.Lefts[at], top + (grid.Row - h) / 2, grid.Widths[at], h));
                    top += grid.Row + grid.Gap;
                }
            }

        return (nodes, grouped ? y + Pad : y);
    }

    /// <summary>
    /// Each declared group's frame around its boxes, its label's room on top once the drawing is turned back.
    /// </summary>
    public static List<PlacedGroup> Frames(GraphSpec spec, IReadOnlyList<PlacedNode> nodes, bool turned)
    {
        (double left, double top) = turned ? (Pad + LabelRoom, Pad) : (Pad, Pad + LabelRoom);
        return
        [
            .. (spec.Groups ?? []).Select(group => (group, members: nodes.Where(node => node.Node.Group == group.Id)
                .ToList()))
            .Where(pair => pair.members.Count > 0)
            .Select(pair =>
            {
                double x = pair.members.Min(node => node.X), y = pair.members.Min(node => node.Y);
                double right = pair.members.Max(node => node.X + node.W);
                double bottom = pair.members.Max(node => node.Y + node.H);
                return new PlacedGroup(pair.group, x - left, y - top, right - x + left + Pad, bottom - y + top + Pad);
            })
        ];
    }

    /// <summary>
    /// The boxes and frames moved so no frame starts before the drawing does, and the drawing grown to hold them.
    /// </summary>
    public static (List<PlacedNode> Nodes, List<PlacedGroup> Groups, double Width, double Height) Fit(
        List<PlacedNode> nodes,
        List<PlacedGroup> groups,
        double width,
        double height)
    {
        if (groups.Count == 0) return (nodes, groups, width, height);

        double dx = Math.Max(0, -groups.Min(group => group.X)), dy = Math.Max(0, -groups.Min(group => group.Y));
        var moved = groups.Select(group => group with
        {
            X = group.X + dx,
            Y = group.Y + dy
        }).ToList();
        return ([
                .. nodes.Select(node => node with
                {
                    X = node.X + dx,
                    Y = node.Y + dy
                })
            ], moved,
            Math.Max(width + dx, moved.Max(group => group.X + group.W)),
            Math.Max(height + dy, moved.Max(group => group.Y + group.H)));
    }

    /// <summary>
    /// Where the layout's columns stand: each one's left and width, a row's height, the gap between two rows, and how
    /// tall a box is.
    /// </summary>
    public sealed record Grid(
        IReadOnlyList<double> Lefts,
        IReadOnlyList<double> Widths,
        double Row,
        double Gap,
        Func<GraphNode, double> Tall);
}
