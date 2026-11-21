using System.Numerics;

namespace LouisAgent.Diagrams.Engine.Geometry.Generic;

/// <summary>A point of our own family, in whichever number type it is given.</summary>
/// <remarks>
/// The seam speaks <c>double</c>, and the point keeps its own number type, so <see cref="Box{T}"/> does its sums
/// in that type and only what is read through the seam is turned into a <c>double</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record Point<T>(T X, T Y) : IPoint where T : INumber<T>
{
    /// <summary>How far across, as the seam reads it.</summary>
    double IPoint.X => double.CreateChecked(X);

    /// <summary>How far down, as the seam reads it.</summary>
    double IPoint.Y => double.CreateChecked(Y);

    /// <summary>The same point, somewhere else.</summary>
    public Point<T> Moved(T across, T down) => new(X + across, Y + down);

    /// <summary>The point turned over the picture's diagonal: across becomes down.</summary>
    public Point<T> Turned() => new(Y, X);

    IPoint IPoint.Moved(double across, double down) => Moved(T.CreateChecked(across), T.CreateChecked(down));

    IPoint IPoint.Turned() => Turned();

    /// <summary>How far it is to another point for a line that runs only across and down.</summary>
    public T StepsTo(Point<T> other) => T.Abs(other.X - X) + T.Abs(other.Y - Y);

    /// <summary>
    /// The point a little way off this one, towards another level with it or straight above or under.
    /// </summary>
    public Point<T> Toward(Point<T> other, T by) =>
        new(X + T.CreateChecked(T.Sign(other.X - X)) * by, Y + T.CreateChecked(T.Sign(other.Y - Y)) * by);

    double IPoint.StepsTo(IPoint other) => double.CreateChecked(StepsTo(Ours(other)));

    IPoint IPoint.Toward(IPoint other, double by) => Toward(Ours(other), T.CreateChecked(by));

    /// <summary>Another point as one of ours: it is one already when the picture is drawn by one geometry.</summary>
    internal static Point<T> Ours(IPoint point) =>
        point as Point<T> ?? new Point<T>(T.CreateChecked(point.X), T.CreateChecked(point.Y));
}
