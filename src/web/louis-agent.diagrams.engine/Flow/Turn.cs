using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Flow;

/// <summary>
/// Turns a diagram laid out to the right over its diagonal, so it runs down (CL60.2): every x becomes a y and every y
/// an x, a box's width its height, and an arrow's way the same way turned.
/// </summary>
/// <remarks>
/// One layout and one router serve both directions: <see cref="GraphLayout"/> lays a down chart out to the right with
/// every box turned, and this turns the drawing back, so the boxes come out their own size and the connectors still
/// bend at right angles. A second, vertical layout would be a second thing to keep in step with the first. A box,
/// a point and a way each turn themselves, so this only says what is turned.
/// </remarks>
internal static class Turn
{
    /// <summary>How far from its line a turned arrow's word starts.</summary>
    private const double BesideGap = 6;

    /// <summary>
    /// The drawing turned over its diagonal. An arrow's word, which stood on its flat piece, now stands beside its
    /// upright line, in the gap between two rows where no box stands, and the drawing grows to hold every word.
    /// </summary>
    public static PlacedGraph Back(PlacedGraph placed, IGeometry geometry)
    {
        var turned = Turned(placed);
        // One word at a time, each clear of the lines and of the words already placed.
        var words = new List<IBox>();
        var edges = new List<PlacedEdge>();
        foreach (var edge in turned.Edges)
        {
            double across = Beside(edge, turned.Edges, words, geometry);
            var beside = edge with { Word = geometry.Point(across, edge.Word.Y) };
            edges.Add(beside);
            if (beside.Edge.Word is not null) words.Add(FlowText.WordBox(beside, geometry));
        }

        double left = edges.Count == 0 ? 0 : Math.Min(0, edges.Min(edge => edge.Word.X - Half(edge)) - BesideGap);
        double right = edges.Count == 0 ? 0 : edges.Max(edge => edge.Word.X + Half(edge)) + BesideGap;
        // The drawing is as wide as what it draws: a waypoint's slot, which draws nothing, leaves no room behind.
        double width = Math.Max(Drawn(turned), right);
        // A word left of a line by the drawing's edge moves everything right a little to stay inside.
        if (left < 0) return Shifted(turned with { Edges = edges }, -left, width);
        return turned with
        {
            Width = (int)Math.Ceiling(width),
            Edges = edges
        };
    }

    /// <summary>How far right the drawing's boxes, frames and lines reach.</summary>
    private static double Drawn(PlacedGraph placed) => placed.Nodes.Select(node => node.Box.Right)
        .Concat(placed.Groups.Select(group => group.Box.Right))
        .Concat(placed.Edges.SelectMany(edge => edge.Way.Stretches).Select(stretch => stretch.Right))
        .DefaultIfEmpty(0).Max();

    /// <summary>
    /// Where a word stands beside its upright line: on its right, unless another line or word is there and the left
    /// is clear (Louis: no word under a line). When neither side is wholly clear, the side clear of words wins, since
    /// a word over a word can't be read at all.
    /// </summary>
    private static double Beside(PlacedEdge edge, IReadOnlyList<PlacedEdge> all, IReadOnlyList<IBox> words,
        IGeometry geometry)
    {
        double rightX = edge.Word.X + BesideGap + Half(edge), leftX = edge.Word.X - BesideGap - Half(edge);
        if (edge.Edge.Word is null) return rightX;
        var ground = FlowText.WordBox(edge, geometry);

        bool ClearOfLines(double middle) => !all.Where(other => other != edge)
            .SelectMany(other => other.Way.Stretches)
            .Any(stretch => stretch.Left < middle + Half(edge) && stretch.Right > middle - Half(edge)
                                                               && stretch.Top < ground.Bottom
                                                               && stretch.Bottom > ground.Top);

        bool ClearOfWords(double middle) => !words.Any(other => other.Left < middle + Half(edge)
                                                                && other.Right > middle - Half(edge)
                                                                && other.Top < ground.Bottom
                                                                && other.Bottom > ground.Top);

        if (ClearOfLines(rightX) && ClearOfWords(rightX)) return rightX;
        if (ClearOfLines(leftX) && ClearOfWords(leftX)) return leftX;
        return ClearOfWords(rightX) || !ClearOfWords(leftX) ? rightX : leftX;
    }

    /// <summary>The drawing moved right, so a word left of a line by its edge stays inside it.</summary>
    private static PlacedGraph Shifted(PlacedGraph placed, double dx, double width) => placed with
    {
        Width = (int)Math.Ceiling(width + dx),
        Nodes = [.. placed.Nodes.Select(node => node with { Box = node.Box.Moved(dx, 0) })],
        Edges =
        [
            .. placed.Edges.Select(edge => edge with
            {
                Way = edge.Way.Moved(dx),
                Word = edge.Word.Moved(dx, 0)
            })
        ],
        Groups = [.. placed.Groups.Select(group => group with { Box = group.Box.Moved(dx, 0) })]
    };

    /// <summary>Half a word's width, in the code font an arrow's word is drawn in.</summary>
    private static double Half(PlacedEdge edge) => edge.Edge.Word is { } word ? FlowText.WordWidth(word) / 2 : 0;

    /// <summary>Every box, way and word of the drawing turned over its diagonal.</summary>
    private static PlacedGraph Turned(PlacedGraph placed) => new(placed.Height, placed.Width,
        [.. placed.Nodes.Select(node => node with { Box = node.Box.Turned() })],
        [
            .. placed.Edges.Select(edge => edge with
            {
                Way = edge.Way.Turned(),
                Word = edge.Word.Turned(),
                Right = placed.Height
            })
        ],
        [.. placed.Groups.Select(group => group with { Box = group.Box.Turned() })]);
}
