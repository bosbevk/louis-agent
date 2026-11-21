using System.Globalization;
using LouisAgent.Diagrams.Engine.Flow.Arrows;

namespace LouisAgent.Diagrams.Tests.Flow.Arrows;

/// <summary>
/// What a test asks of an arrow's way: its places and its turns, as the picture writes them, to one decimal. A
/// test asks the way itself, where it used to read the way's text back number by number (CL65.4, ADR 0123).
/// </summary>
internal static class Ways
{
    /// <summary>Every place of a way, in the order the line runs: a turn's corner and its end each as one.</summary>
    public static List<(double X, double Y)> Points(ArrowPath way) =>
        [.. way.Pieces.Select(piece => At(piece.X, piece.Y))];

    /// <summary>How far across each place of a way is.</summary>
    public static List<double> Xs(ArrowPath way) => [.. way.Pieces.Select(piece => Written(piece.X))];

    /// <summary>How far down each place of a way is.</summary>
    public static List<double> Ys(ArrowPath way) => [.. way.Pieces.Select(piece => Written(piece.Y))];

    /// <summary>Every number of a way, across then down for each place in turn.</summary>
    public static List<double> Numbers(ArrowPath way) =>
        [.. way.Pieces.SelectMany(piece => new[] { Written(piece.X), Written(piece.Y) })];

    /// <summary>The corners a way turns through, in the order the line runs.</summary>
    public static List<(double X, double Y)> Corners(ArrowPath way) =>
        [.. way.Pieces.Where(piece => piece.Turn == ArrowTurn.Corner).Select(piece => At(piece.X, piece.Y))];

    /// <summary>How many rounded turns a way has. A way with none is one straight line or several.</summary>
    public static int Turns(ArrowPath way) => way.Pieces.Count(piece => piece.Turn == ArrowTurn.Corner);

    /// <summary>
    /// How far before each corner its turn starts: from the end of the straight piece to the corner itself.
    /// </summary>
    public static IEnumerable<double> TurnStarts(ArrowPath way) => way.Pieces.Zip(way.Pieces.Skip(1))
        .Where(pair => pair.Second.Turn == ArrowTurn.Corner)
        .Select(pair => Math.Abs(Written(pair.Second.X) - Written(pair.First.X))
                        + Math.Abs(Written(pair.Second.Y) - Written(pair.First.Y)));

    /// <summary>A number as a picture writes it: to one decimal.</summary>
    public static double Written(double value) =>
        double.Parse(value.ToString("0.#", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    /// <summary>A place as a picture writes it.</summary>
    public static (double X, double Y) At(double x, double y) => (Written(x), Written(y));
}
