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
}
