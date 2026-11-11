using System.Globalization;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Placed;

namespace LouisAgent.DocDiagrams.Tests;

/// <summary>
/// What a reader would see wrong in a placed flow (CL60.2, Louis: "you must look at each one"): a word outside the
/// drawing or on a box, and a line through a box it doesn't join.
/// </summary>
internal static class Clearance
{
    /// <summary>Every fault in the drawing, said in words.</summary>
    public static IEnumerable<string> Faults(PlacedGraph placed)
    {
        foreach (var edge in placed.Edges)
        {
            foreach (var box in placed.Nodes.Where(box => box.Node.Id != edge.Edge.From && box.Node.Id != edge.Edge.To))
                if (Segments(edge.Path).Any(segment => Crosses(segment, box)))
                    yield return $"the line {edge.Edge.From}>{edge.Edge.To} crosses {box.Node.Id}";

            if (edge.Edge.Word is not { } word) continue;
            var (x, y, w, h) = WordBox(edge, word);
            if (x < 0 || y < 0 || x + w > placed.Width || y + h > placed.Height)
                yield return $"the word '{word}' leaves the drawing";
            foreach (var box in placed.Nodes.Where(box => x < box.X + box.W && box.X < x + w && y < box.Y + box.H
                                                          && box.Y < y + h))
                yield return $"the word '{word}' sits on {box.Node.Id}";
            foreach (var other in placed.Edges.Where(other => other != edge
                                                              && Segments(other.Path).Any(piece =>
                                                                  Covers(piece, x, y, w, h))))
                yield return $"the word '{word}' lies on the line {other.Edge.From}>{other.Edge.To}";
        }
    }

    /// <summary>The ground a word covers, drawn centred on its point and just above it.</summary>
    private static (double X, double Y, double W, double H) WordBox(PlacedEdge edge, string word)
    {
        double width = FlowText.WordWidth(word);
        return (edge.WordX - width / 2, edge.WordY - 16, width, FlowText.WordStep * FlowText.WordLines(word).Count);
    }

    /// <summary>The straight pieces of a path: a curve's corner counts as the point it turns at.</summary>
    private static IEnumerable<((double X, double Y) A, (double X, double Y) B)> Segments(string path)
    {
        var numbers = path.Split(' ')
            .Where(part => double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToList();
        var points = Enumerable.Range(0, numbers.Count / 2).Select(at => (numbers[2 * at], numbers[2 * at + 1]))
            .ToList();
        return points.Zip(points.Skip(1));
    }

    /// <summary>Whether a straight piece passes through the ground a word covers.</summary>
    private static bool Covers(((double X, double Y) A, (double X, double Y) B) piece, double x, double y, double w,
        double h) =>
        Math.Min(piece.A.X, piece.B.X) < x + w && Math.Max(piece.A.X, piece.B.X) > x
                                               && Math.Min(piece.A.Y, piece.B.Y) < y + h &&
                                               Math.Max(piece.A.Y, piece.B.Y) > y;

    /// <summary>Whether a straight piece passes through a box's inside, its edges a pixel in.</summary>
    private static bool Crosses(((double X, double Y) A, (double X, double Y) B) segment, PlacedNode box)
    {
        double left = box.X + 1, right = box.X + box.W - 1, top = box.Y + 1, bottom = box.Y + box.H - 1;
        double minX = Math.Min(segment.A.X, segment.B.X), maxX = Math.Max(segment.A.X, segment.B.X);
        double minY = Math.Min(segment.A.Y, segment.B.Y), maxY = Math.Max(segment.A.Y, segment.B.Y);
        return minX < right && maxX > left && minY < bottom && maxY > top;
    }
}
