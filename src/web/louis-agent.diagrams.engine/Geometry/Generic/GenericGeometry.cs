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

    /// <summary>
    /// The smallest box of ours that holds every one of these: from the least left and top to the most right and
    /// bottom, each taken from the boxes themselves, so nothing is rounded on the way.
    /// </summary>
    public IBox Around(IEnumerable<IBox> boxes)
    {
        var all = boxes.Select(box => (Box<T>)Box(box.Left, box.Top, box.Width, box.Height)).ToList();
        T left = all.Min(box => box.Left)!, top = all.Min(box => box.Top)!;
        T right = all.Max(box => box.Right)!, bottom = all.Max(box => box.Bottom)!;
        return new Box<T>(left, top, right - left, bottom - top);
    }
}
