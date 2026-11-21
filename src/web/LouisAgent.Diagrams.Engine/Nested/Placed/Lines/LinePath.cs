using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Paths;

namespace LouisAgent.Diagrams.Engine.Nested.Placed.Lines;

/// <summary>
/// A placed line as the path an SVG draws (F134.7): straight from point to point, with each corner rounded.
/// </summary>
public static class LinePath
{
    /// <summary>How far before a corner the line starts to turn.</summary>
    private const double Turn = 9;

    /// <summary>The path's data, such as <c>M10 20L40 20</c>.</summary>
    public static string Of(IReadOnlyList<IPoint> points)
    {
        var path = new PathWriter().From(points[0].X, points[0].Y);
        for (int at = 1; at < points.Count - 1; at++)
        {
            // A corner: stop a little before it, and curve through it to a little after.
            IPoint corner = points[at], before = corner.Toward(points[at - 1], Turn);
            var after = corner.Toward(points[at + 1], Turn);
            path.To(before.X, before.Y).Curve(corner.X, corner.Y, after.X, after.Y);
        }

        return path.To(points[^1].X, points[^1].Y).Text;
    }
}
