using System.Globalization;
using LouisAgent.Web.Graphs.Placed;

namespace LouisAgent.Web.Graphs;

/// <summary>
/// Gives every box and arrow of a diagram its place (F111): the columns left to right across the width, a column's
/// boxes stacked and centred, and each arrow a curve from one box's right edge to the next box's left edge.
/// </summary>
/// <remarks>
/// A pure function over the data, so it is tested with numbers and never needs a browser. An arrow that names a box
/// the diagram doesn't have is left out, and the page's own test holds its ids.
/// </remarks>
public static class GraphLayout
{
    private const double BoxWidth = 180;
    private const double BoxHeight = 56;
    private const double NotedBoxHeight = 72;
    private const double Gap = 18;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>Places a diagram across the given width.</summary>
    public static PlacedGraph Place(GraphSpec spec, int width = 880)
    {
        // Each column's boxes in order, and the tallest column sets the height.
        var columns = spec.Nodes.GroupBy(node => node.Column).OrderBy(column => column.Key).ToList();
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

        // An arrow only between two boxes that exist.
        var byId = nodes.ToDictionary(placed => placed.Node.Id);
        var edges = spec.Edges
            .Where(edge => byId.ContainsKey(edge.From) && byId.ContainsKey(edge.To))
            .Select(edge => Curve(edge, byId[edge.From], byId[edge.To]))
            .ToList();

        return new PlacedGraph(width, (int)Math.Ceiling(height), nodes, edges);
    }

    private static double HeightOf(GraphNode node) => node.Note is null ? BoxHeight : NotedBoxHeight;

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
        // The curve's middle: a cubic at t = ½ is the weighted mean of its four points.
        double midX = (x1 + 3 * (x1 + bend) + 3 * (x2 - bend) + x2) / 8, midY = (y1 + 3 * y1 + 3 * y2 + y2) / 8;
        return new PlacedEdge(edge, path, Length(x1, y1, x1 + bend, y1, x2 - bend, y2, x2, y2), midX, midY);
    }

    private static string N(double value) => value.ToString("0.#", Invariant);

    /// <summary>The curve's length as the sum of sixteen straight pieces along it.</summary>
    private static double Length(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
    {
        const int pieces = 16;
        double length = 0, lastX = x0, lastY = y0;
        for (int piece = 1; piece <= pieces; piece++)
        {
            double t = (double)piece / pieces, u = 1 - t;
            double x = u * u * u * x0 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x3;
            double y = u * u * u * y0 + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * y3;
            length += Math.Sqrt((x - lastX) * (x - lastX) + (y - lastY) * (y - lastY));
            (lastX, lastY) = (x, y);
        }

        return length;
    }
}
