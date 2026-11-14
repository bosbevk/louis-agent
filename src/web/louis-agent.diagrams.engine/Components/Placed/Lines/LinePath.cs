using System.Globalization;
using System.Text;

namespace LouisAgent.Diagrams.Engine.Components.Placed.Lines;

/// <summary>
/// A placed line as the path an SVG draws (F134.7): straight from point to point, with each corner rounded.
/// </summary>
public static class LinePath
{
    /// <summary>How far before a corner the line starts to turn.</summary>
    private const double Turn = 9;

    /// <summary>The path's data, such as <c>M10 20L40 20</c>.</summary>
    public static string Of(IReadOnlyList<LinePoint> points)
    {
        var path = new StringBuilder();
        path.Append(CultureInfo.InvariantCulture, $"M{points[0].X:0.#} {points[0].Y:0.#}");
        for (int at = 1; at < points.Count; at++)
        {
            var point = points[at];
            if (at == points.Count - 1)
            {
                path.Append(CultureInfo.InvariantCulture, $"L{point.X:0.#} {point.Y:0.#}");
                continue;
            }

            // A corner: stop a little before it, and curve through it to a little after.
            var before = Toward(point, points[at - 1]);
            var after = Toward(point, points[at + 1]);
            path.Append(CultureInfo.InvariantCulture,
                $"L{before.X:0.#} {before.Y:0.#}Q{point.X:0.#} {point.Y:0.#} {after.X:0.#} {after.Y:0.#}");
        }

        return path.ToString();
    }

    /// <summary>The point a turn's length from a corner, on the way to its neighbour.</summary>
    private static LinePoint Toward(LinePoint corner, LinePoint neighbour) => new(
        corner.X + Turn * Math.Sign(neighbour.X - corner.X),
        corner.Y + Turn * Math.Sign(neighbour.Y - corner.Y));
}
