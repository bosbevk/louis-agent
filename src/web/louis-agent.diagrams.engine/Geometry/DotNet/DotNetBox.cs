using System.Drawing;
using System.Numerics;

namespace LouisAgent.Diagrams.Engine.Geometry.DotNet;

/// <summary>
/// A box of Microsoft's family: a <see cref="RectangleF"/>, which has a box's sums ready-made, read through the
/// seam.
/// </summary>
/// <remarks>
/// Kept beside our own <c>Box&lt;T&gt;</c> and not in use (Louis: "keep ours and MS's but use ours."). Its
/// numbers are single precision, so what it answers can differ from ours in <c>double</c> in the last digits.
/// The sums are Microsoft's own: <see cref="RectangleF.Inflate(RectangleF, float, float)"/> for room,
/// <see cref="RectangleF.Union"/> for two boxes joined, and <see cref="Vector2"/>'s for the middle.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/dotnet/api/system.drawing.rectanglef">RectangleF (Microsoft Learn)</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record DotNetBox(RectangleF Rectangle) : IBox
{
    /// <summary>Its left edge.</summary>
    public double Left => Rectangle.Left;

    /// <summary>Its top edge.</summary>
    public double Top => Rectangle.Top;

    /// <summary>How wide it is.</summary>
    public double Width => Rectangle.Width;

    /// <summary>How tall it is.</summary>
    public double Height => Rectangle.Height;

    /// <summary>Its right edge.</summary>
    public double Right => Rectangle.Right;

    /// <summary>Its bottom edge.</summary>
    public double Bottom => Rectangle.Bottom;

    /// <summary>The point in its middle: its corner, and half its size on from there.</summary>
    public IPoint Middle => new DotNetPoint(Rectangle.Location.ToVector2() + Rectangle.Size.ToVector2() / 2);

    /// <summary>The box with room added on every side.</summary>
    public IBox Grown(double by) => new DotNetBox(RectangleF.Inflate(Rectangle, (float)by, (float)by));

    /// <summary>The same box, somewhere else.</summary>
    public IBox Moved(double across, double down)
    {
        // A RectangleF is a struct that moves itself, so a copy is moved and this box stays where it is.
        var moved = Rectangle;
        moved.Offset((float)across, (float)down);
        return new DotNetBox(moved);
    }

    /// <summary>The smallest box that holds this one and another.</summary>
    public IBox With(IBox other) => new DotNetBox(RectangleF.Union(Rectangle, Theirs(other)));

    /// <summary>The box with room added on each side, its own on every one.</summary>
    public IBox Grown(double left, double top, double right, double bottom) => new DotNetBox(new RectangleF(
        Rectangle.Left - (float)left, Rectangle.Top - (float)top, Rectangle.Width + (float)left + (float)right,
        Rectangle.Height + (float)top + (float)bottom));

    /// <summary>The same box with its middle on a line across the picture.</summary>
    public IBox OnLine(double y) => new DotNetBox(Rectangle with { Y = (float)y - Rectangle.Height / 2 });

    /// <summary>The box turned over its diagonal.</summary>
    public IBox Turned() =>
        new DotNetBox(new RectangleF(Rectangle.Top, Rectangle.Left, Rectangle.Height, Rectangle.Width));

    /// <summary>A place along the box from its left.</summary>
    public double Across(double part) => Rectangle.Left + Rectangle.Width * (float)part;

    /// <summary>A place down the box from its top.</summary>
    public double Down(double part) => Rectangle.Top + Rectangle.Height * (float)part;

    /// <summary>
    /// Another box as a rectangle: its own when it is of this family, read through the seam when it is not.
    /// </summary>
    private static RectangleF Theirs(IBox box) => box is DotNetBox ours
        ? ours.Rectangle
        : new RectangleF((float)box.Left, (float)box.Top, (float)box.Width, (float)box.Height);
}
