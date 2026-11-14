using System.Text;

namespace LouisAgent.Diagrams.Engine.Svg.Paths;

/// <summary>
/// Writes the path an SVG draws, piece by piece: from a point, a line, a curve, an arc. It knows SVG's letters
/// and how a number is written, so a drawing says what the line does and never builds its text.
/// </summary>
/// <remarks>
/// <para>
/// Builder (Refactoring.Guru): twenty places built this text by hand, each with its own letters and its own
/// call for every number. Here a path is told its pieces and gives its text at the end.
/// </para>
/// <para>
/// It writes what those places wrote, so no picture changes: a letter and its numbers, with nothing between
/// two pieces. A flow's arrow is the one path with room round every letter, since other code reads that text
/// back, and it says so with <c>spaced</c>.
/// </para>
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed class PathWriter(bool spaced = false)
{
    private readonly StringBuilder _text = new();

    /// <summary>The path's text, as an SVG's <c>d</c> holds it.</summary>
    public string Text => _text.ToString();

    /// <summary>Starts at a point.</summary>
    public PathWriter From(double x, double y) => Piece('M', x, y);

    /// <summary>A straight line to a point.</summary>
    public PathWriter To(double x, double y) => Piece('L', x, y);

    /// <summary>A line straight across, to a place.</summary>
    public PathWriter Across(double x) => Piece('H', x);

    /// <summary>A line straight down or up, to a height.</summary>
    public PathWriter Down(double y) => Piece('V', y);

    /// <summary>A curve to a point, pulled toward the point it bends through.</summary>
    public PathWriter Curve(double throughX, double throughY, double x, double y) =>
        Piece('Q', throughX, throughY, x, y);

    /// <summary>
    /// An arc to a point, of an oval with these radii: the long way round or the short, and turning with the
    /// clock or against it.
    /// </summary>
    public PathWriter Arc(double radiusX, double radiusY, bool large, bool sweep, double x, double y) =>
        Piece('A', radiusX, radiusY, 0, large ? 1 : 0, sweep ? 1 : 0, x, y);

    /// <summary>The same arc, by how far across and down it goes from where the path is.</summary>
    public PathWriter ArcBy(double radiusX, double radiusY, bool large, bool sweep, double across, double down) =>
        Piece('a', radiusX, radiusY, 0, large ? 1 : 0, sweep ? 1 : 0, across, down);

    /// <summary>Back to where it started, closing the shape.</summary>
    public PathWriter Close() => Piece('Z');

    /// <summary>
    /// One piece: its letter and its numbers. A spaced path has room before every piece but the first, and
    /// after the letter.
    /// </summary>
    private PathWriter Piece(char letter, params double[] numbers)
    {
        if (spaced && _text.Length > 0) _text.Append(' ');
        _text.Append(letter);
        if (spaced && numbers.Length > 0) _text.Append(' ');
        _text.AppendJoin(' ', numbers.Select(SvgWriter.N));
        return this;
    }
}
