using System.Globalization;
using LouisAgent.Diagrams.Flow.Placed;

namespace LouisAgent.Diagrams.Flow;

/// <summary>
/// Turns a diagram laid out to the right over its diagonal, so it runs down (CL60.2): every x becomes a y and every y
/// an x, a box's width its height, and an arrow's path the same path turned.
/// </summary>
/// <remarks>
/// One layout and one router serve both directions: <see cref="GraphLayout"/> lays a down chart out to the right with
/// every box turned, and this turns the drawing back, so the boxes come out their own size and the connectors still
/// bend at right angles. A second, vertical layout would be a second thing to keep in step with the first.
/// </remarks>
internal static class Turn
{
    /// <summary>The drawing turned over its diagonal.</summary>
    public static PlacedGraph Back(PlacedGraph placed) => new(placed.Height, placed.Width,
        [.. placed.Nodes.Select(node => node with { X = node.Y, Y = node.X, W = node.H, H = node.W })],
        [
            .. placed.Edges.Select(edge => edge with
            {
                Path = PathOf(edge.Path),
                WordX = edge.WordY,
                WordY = edge.WordX,
                Right = placed.Height
            })
        ]);

    /// <summary>
    /// A path turned: its commands kept, each x and y after them swapped. The router writes only <c>M</c>, <c>L</c> and
    /// <c>Q</c>, whose numbers all come in x, y pairs.
    /// </summary>
    private static string PathOf(string path)
    {
        var parts = path.Split(' ');
        var turned = new List<string>(parts.Length);
        for (int at = 0; at < parts.Length; at++)
        {
            if (!double.TryParse(parts[at], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                turned.Add(parts[at]);
                continue;
            }

            turned.Add(parts[at + 1]);
            turned.Add(parts[at]);
            at++;
        }

        return string.Join(' ', turned);
    }
}
