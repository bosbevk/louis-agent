using System.Numerics;

namespace LouisAgent.Diagrams.Engine.Geometry.Generic;

/// <summary>
/// Makes boxes and points of our own family, in the number type it is given. In <c>double</c> it is the geometry
/// the engine draws with.
/// </summary>
/// <remarks>The factory of our family (Abstract Factory): what it makes is a <see cref="Box{T}"/> or a
/// <see cref="Point{T}"/> and nothing of another family.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed class GenericGeometry<T> : IGeometry where T : INumber<T>
{
    /// <summary>A box of ours.</summary>
    public IBox Box(double x, double y, double width, double height) => new Box<T>(T.CreateChecked(x),
        T.CreateChecked(y), T.CreateChecked(width), T.CreateChecked(height));

    /// <summary>A point of ours.</summary>
    public IPoint Point(double x, double y) => new Point<T>(T.CreateChecked(x), T.CreateChecked(y));
}
