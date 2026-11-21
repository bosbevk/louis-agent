using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Paths;

namespace LouisAgent.Diagrams.Engine.Flow.Arrows;

/// <summary>
/// An arrow's way as an SVG draws it: where it starts, then each place on from there, the end of a straight
/// piece or a rounded turn through a corner. It knows how it is turned, moved and joined to another.
/// </summary>
/// <remarks>
/// <para>
/// Value Object (Fowler), as the cure for Primitive Obsession: a way was a text, and the class that turns a
/// picture, the one that joins a long arrow's pieces, and two drawings each read it back number by number. What
/// they worked out is asked of the way now, and its text is written once, at the end.
/// </para>
/// <para>
/// Its numbers are the layout's own, exact, and are rounded only where its text is written. Two ways are the same
/// when they write the same text.
/// </para>
/// </remarks>
/// <param name="Pieces">Its places, in the order the line runs.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record ArrowPath(IReadOnlyList<ArrowPiece> Pieces)
{
    /// <summary>A way that starts at a point.</summary>
    public static ArrowPath From(double x, double y) => new([Piece(ArrowTurn.Start, x, y)]);

    /// <summary>A way that starts at a point of the layout.</summary>
    public static ArrowPath From(IPoint start) => From(start.X, start.Y);

    /// <summary>The way with a straight piece on to a point.</summary>
    public ArrowPath To(double x, double y) => new([.. Pieces, Piece(ArrowTurn.Line, x, y)]);

    /// <summary>The way with a straight piece on to a point of the layout.</summary>
    public ArrowPath To(IPoint point) => To(point.X, point.Y);

    /// <summary>The way with a rounded turn through a corner of the layout, to the point where the turn ends.</summary>
    public ArrowPath Round(IPoint corner, IPoint after) => Round(corner.X, corner.Y, after.X, after.Y);

    /// <summary>The way with a rounded turn: a curve through a corner, to the point where the turn ends.</summary>
    public ArrowPath Round(double cornerX, double cornerY, double x, double y) =>
        new([.. Pieces, Piece(ArrowTurn.Corner, cornerX, cornerY), Piece(ArrowTurn.After, x, y)]);

    /// <summary>
    /// The way with another joined on: the second's start becomes a place the line runs straight on to, as a
    /// long arrow's pieces are made one across each waypoint.
    /// </summary>
    public ArrowPath Then(ArrowPath next) =>
        new([.. Pieces, next.Start with { Turn = ArrowTurn.Line }, .. next.Pieces.Skip(1)]);

    /// <summary>The way turned over the picture's diagonal: every place's across becomes its down.</summary>
    public ArrowPath Turned() => new([.. Pieces.Select(piece => piece with { X = piece.Y, Y = piece.X })]);

    /// <summary>The way moved across.</summary>
    public ArrowPath Moved(double across) => new([.. Pieces.Select(piece => piece with { X = piece.X + across })]);

    /// <summary>Where it starts.</summary>
    public ArrowPiece Start => Pieces[0];

    /// <summary>Its stretches, each from one place to the next, in the order the line runs.</summary>
    public IEnumerable<ArrowStretch> Stretches =>
        Pieces.Zip(Pieces.Skip(1), (from, to) => new ArrowStretch(from, to));

    /// <summary>Its text, as an SVG's <c>d</c> holds it.</summary>
    public string Text
    {
        get
        {
            var path = new PathWriter();
            for (int at = 0; at < Pieces.Count; at++)
            {
                var piece = Pieces[at];
                // A turn is two places, its corner and where it ends, and one curve in the text.
                if (piece.Turn == ArrowTurn.Corner)
                {
                    var after = Pieces[++at];
                    path.Curve(piece.X, piece.Y, after.X, after.Y);
                    continue;
                }

                if (piece.Turn == ArrowTurn.Start) path.From(piece.X, piece.Y);
                else path.To(piece.X, piece.Y);
            }

            return path.Text;
        }
    }

    /// <summary>A way is its places: two that write the same text are the same way.</summary>
    public bool Equals(ArrowPath? other) =>
        other is not null && string.Equals(Text, other.Text, StringComparison.Ordinal);

    /// <summary>The hash of its text, so two ways that are the same hash the same.</summary>
    public override int GetHashCode() => Text.GetHashCode(StringComparison.Ordinal);

    /// <summary>A place of a way.</summary>
    private static ArrowPiece Piece(ArrowTurn turn, double x, double y) => new(turn, x, y);
}
