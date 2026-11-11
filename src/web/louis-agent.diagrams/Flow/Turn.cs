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
    /// <summary>How far from its line a turned arrow's word starts.</summary>
    private const double BesideGap = 6;

    /// <summary>
    /// The drawing turned over its diagonal. An arrow's word, which stood on its flat piece, now stands beside its
    /// upright line, in the gap between two rows where no box stands, and the drawing grows to hold every word.
    /// </summary>
    public static PlacedGraph Back(PlacedGraph placed)
    {
        var turned = Turned(placed);
        // One word at a time, each clear of the lines and of the words already placed.
        var words = new List<(double Left, double Right, double Top, double Bottom)>();
        var edges = new List<PlacedEdge>();
        foreach (var edge in turned.Edges)
        {
            var beside = edge with { WordX = Beside(edge, turned.Edges, words) };
            edges.Add(beside);
            var (x, y, w, h) = FlowText.WordBox(beside);
            if (beside.Edge.Word is not null) words.Add((x, x + w, y, y + h));
        }

        double left = edges.Count == 0 ? 0 : Math.Min(0, edges.Min(edge => edge.WordX - Half(edge)) - BesideGap);
        double right = edges.Count == 0 ? 0 : edges.Max(edge => edge.WordX + Half(edge));
        // A word left of a line by the drawing's edge moves everything right a little to stay inside.
        if (left < 0) return Shifted(turned with { Edges = edges }, -left, right);
        return turned with
        {
            Width = Math.Max(turned.Width, (int)Math.Ceiling(right + BesideGap)),
            Edges = edges
        };
    }

    /// <summary>
    /// Where a word stands beside its upright line: on its right, unless another line or word is there and the left
    /// is clear (Louis: no word under a line). When neither side is wholly clear, the side clear of words wins, since
    /// a word over a word can't be read at all.
    /// </summary>
    private static double Beside(PlacedEdge edge, IReadOnlyList<PlacedEdge> all,
        IReadOnlyList<(double Left, double Right, double Top, double Bottom)> words)
    {
        double rightX = edge.WordX + BesideGap + Half(edge), leftX = edge.WordX - BesideGap - Half(edge);
        if (edge.Edge.Word is null) return rightX;
        var (_, top, _, height) = FlowText.WordBox(edge);
        double bottom = top + height;

        bool ClearOfLines(double middle) => !all.Where(other => other != edge)
            .SelectMany(other => Pieces(other.Path))
            .Any(piece => Math.Min(piece.A.X, piece.B.X) < middle + Half(edge)
                          && Math.Max(piece.A.X, piece.B.X) > middle - Half(edge)
                          && Math.Min(piece.A.Y, piece.B.Y) < bottom && Math.Max(piece.A.Y, piece.B.Y) > top);

        bool ClearOfWords(double middle) => !words.Any(other => other.Left < middle + Half(edge)
                                                                && other.Right > middle - Half(edge)
                                                                && other.Top < bottom && other.Bottom > top);

        if (ClearOfLines(rightX) && ClearOfWords(rightX)) return rightX;
        if (ClearOfLines(leftX) && ClearOfWords(leftX)) return leftX;
        return ClearOfWords(rightX) || !ClearOfWords(leftX) ? rightX : leftX;
    }

    /// <summary>A path's straight pieces, a curve's corner counted as the point it turns at.</summary>
    private static IEnumerable<((double X, double Y) A, (double X, double Y) B)> Pieces(string path)
    {
        var numbers = path.Split(' ')
            .Where(part => double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToList();
        var points = Enumerable.Range(0, numbers.Count / 2).Select(at => (numbers[2 * at], numbers[2 * at + 1]))
            .ToList();
        return points.Zip(points.Skip(1));
    }

    /// <summary>The drawing moved right, so a word left of a line by its edge stays inside it.</summary>
    private static PlacedGraph Shifted(PlacedGraph placed, double dx, double right) => placed with
    {
        Width = (int)Math.Ceiling(Math.Max(placed.Width, right + BesideGap) + dx),
        Nodes = [.. placed.Nodes.Select(node => node with { X = node.X + dx })],
        Edges =
        [
            .. placed.Edges.Select(edge => edge with
            {
                Path = Moved(edge.Path, dx),
                WordX = edge.WordX + dx
            })
        ],
        Groups = [.. placed.Groups.Select(group => group with { X = group.X + dx })]
    };

    /// <summary>A path moved right: the first number of each pair after a command, its x, grows.</summary>
    private static string Moved(string path, double dx)
    {
        var parts = path.Split(' ');
        bool x = true;
        for (int at = 0; at < parts.Length; at++)
        {
            if (!double.TryParse(parts[at], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                continue;
            if (x) parts[at] = (value + dx).ToString("0.#", CultureInfo.InvariantCulture);
            x = !x;
        }

        return string.Join(' ', parts);
    }

    /// <summary>Half a word's width, in the code font an arrow's word is drawn in.</summary>
    private static double Half(PlacedEdge edge) => edge.Edge.Word is { } word ? FlowText.WordWidth(word) / 2 : 0;

    /// <summary>Every point of the drawing swapped over its diagonal.</summary>
    private static PlacedGraph Turned(PlacedGraph placed) => new(placed.Height, placed.Width,
        [
            .. placed.Nodes.Select(node => node with
            {
                X = node.Y,
                Y = node.X,
                W = node.H,
                H = node.W
            })
        ],
        [
            .. placed.Edges.Select(edge => edge with
            {
                Path = PathOf(edge.Path),
                WordX = edge.WordY,
                WordY = edge.WordX,
                Right = placed.Height
            })
        ],
        [
            .. placed.Groups.Select(group => group with
            {
                X = group.Y,
                Y = group.X,
                W = group.H,
                H = group.W
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
