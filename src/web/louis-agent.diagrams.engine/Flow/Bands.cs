using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Flow.Rows;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Flow;

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

    /// <summary>
    /// Every box placed on the grid, band by band, and the drawing's height. How tall a band's rows are and which
    /// row a box stands on is <see cref="LevelRows"/>'s to say. The boxes are of the geometry handed in.
    /// </summary>
    public static (List<PlacedNode> Nodes, double Height) Place(
        GraphSpec spec,
        List<List<GraphNode>> columns,
        Grid grid,
        bool turned,
        IGeometry geometry)
    {
        bool grouped = spec.Groups is { Count: > 0 };
        string? Key(GraphNode node) => grouped ? node.Group : null;

        // The bands: the columns each has boxes in, and as many rows as its fullest column has boxes.
        var keys = spec.Nodes.Select(Key).Distinct().ToList();
        var spans = keys.Select(key => Enumerable.Range(0, columns.Count)
            .Where(at => columns[at].Any(node => Key(node) == key)).ToHashSet()).ToList();
        var rows = keys.Select(key => columns.Max(column => column.Count(node => Key(node) == key))).ToList();

        // How tall each band's rows are.
        var shapes = keys.Select((key, band) => new BandShape(
            spec.Nodes.Where(node => Key(node) == key).Max(grid.Tall), spans[band], rows[band])).ToList();
        var row = LevelRows.RowHeights(shapes, grid.Gap);
        double Tall(int band, int boxes) => boxes * row[band] + grid.Gap * (boxes - 1);

        // Each band as tall as its rows. A band stands under the earlier bands it shares a column with, with room for
        // their frames; one that shares none starts at the top beside them (Louis, of the checks chart: no empty
        // corner beside a group).
        double start = (grouped, turned) switch
        {
            (false, _) => 0,
            (true, false) => Pad + LabelRoom,
            (true, true) => Pad
        };
        var tops = new List<double>();
        var bottoms = new List<double>();
        for (int band = 0; band < keys.Count; band++)
        {
            double top = Enumerable.Range(0, band).Where(earlier => spans[earlier].Overlaps(spans[band]))
                .Select(earlier => bottoms[earlier] + Between).DefaultIfEmpty(start).Max();
            tops.Add(top);
            bottoms.Add(top + Tall(band, rows[band]));
        }

        double y = bottoms.DefaultIfEmpty(0).Max();

        // Each box stands on the row level with what feeds it, in the middle of that row.
        var rowGrid = new RowGrid(columns, node => keys.IndexOf(Key(node)), tops, row, rows, grid.Gap, spec.Edges);
        var rowTops = LevelRows.RowTops(rowGrid);
        var nodes = new List<PlacedNode>();
        for (int at = 0; at < columns.Count; at++)
            for (int band = 0; band < keys.Count; band++)
                foreach (var node in columns[at].Where(node => Key(node) == keys[band]))
                {
                    double h = grid.Tall(node);
                    double top = rowTops[node.Id] + (row[band] - h) / 2;
                    nodes.Add(new PlacedNode(node, geometry.Box(grid.Lefts[at], top, grid.Widths[at], h)));
                }

        return (nodes, grouped ? y + Pad : y);
    }

    /// <summary>
    /// Each declared group's frame around its boxes, its label's room on top once the drawing is turned back.
    /// </summary>
    public static List<PlacedGroup> Frames(GraphSpec spec, IReadOnlyList<PlacedNode> nodes, bool turned,
        IGeometry geometry)
    {
        (double left, double top) = turned ? (Pad + LabelRoom, Pad) : (Pad, Pad + LabelRoom);

        // The frame is the smallest box round the group's boxes, grown by the room each side keeps.
        return
        [
            .. (spec.Groups ?? []).Select(group => (group, members: nodes.Where(node => node.Node.Group == group.Id)
                .ToList()))
            .Where(pair => pair.members.Count > 0)
            .Select(pair => new PlacedGroup(pair.group,
                geometry.Around(pair.members.Select(node => node.Box)).Grown(left, top, Pad, Pad)))
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

        double dx = Math.Max(0, -groups.Min(group => group.Box.Left));
        double dy = Math.Max(0, -groups.Min(group => group.Box.Top));
        var moved = groups.Select(group => group with { Box = group.Box.Moved(dx, dy) }).ToList();
        return ([.. nodes.Select(node => node with { Box = node.Box.Moved(dx, dy) })], moved,
            Math.Max(width + dx, moved.Max(group => group.Box.Right)),
            Math.Max(height + dy, moved.Max(group => group.Box.Bottom)));
    }

    /// <summary>
    /// Where the layout's columns stand: each one's left and width, the gap between two rows, and how tall a box is.
    /// </summary>
    public sealed record Grid(
        IReadOnlyList<double> Lefts,
        IReadOnlyList<double> Widths,
        double Gap,
        Func<GraphNode, double> Tall);
}
