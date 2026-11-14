using System.Numerics;

namespace LouisAgent.Diagrams.Engine.Geometry.Generic;

/// <summary>
/// A box of our own family, which does its sums in whichever number type it is given. The engine uses it in
/// <c>double</c>.
/// </summary>
/// <remarks>
/// <para>
/// Generic math (.NET's <see cref="INumber{TSelf}"/>), Louis's decision for our own side of the seam: "our own
/// will be Box&lt;T&gt;". The sums are written once, with <c>+</c> and <c>/</c> over any number, where a box in
/// <c>double</c> and one in <c>float</c> would have been the same lines twice. Run in <c>float</c> beside
/// Microsoft's box, it shows what in a difference is single precision's doing.
/// </para>
/// <para>
/// Each sum is written the way the layouts wrote it by hand, <c>left + width / 2</c> and <c>width + 2 * room</c>,
/// so in <c>double</c> it answers to the last digit what they answered, and no picture changes.
/// </para>
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record Box<T>(T Left, T Top, T Width, T Height) : IBox where T : INumber<T>
{
    /// <summary>Two, in the box's own number type, for a half and for room on both sides.</summary>
    private static readonly T Two = T.CreateChecked(2);

    /// <summary>Its right edge.</summary>
    public T Right => Left + Width;

    /// <summary>Its bottom edge.</summary>
    public T Bottom => Top + Height;

    /// <summary>The point in its middle.</summary>
    public Point<T> Middle => new(Left + Width / Two, Top + Height / Two);

    /// <summary>The box with room added on every side.</summary>
    public Box<T> Grown(T by) => new(Left - by, Top - by, Width + Two * by, Height + Two * by);

    /// <summary>The box with room added on each side, its own on every one.</summary>
    public Box<T> Grown(T left, T top, T right, T bottom) =>
        new(Left - left, Top - top, Width + left + right, Height + top + bottom);

    /// <summary>The same box, somewhere else.</summary>
    public Box<T> Moved(T across, T down) => this with { Left = Left + across, Top = Top + down };

    /// <summary>The same box with its middle on a line across the picture, at this height.</summary>
    public Box<T> OnLine(T y) => this with { Top = y - Height / Two };

    /// <summary>The box turned over its diagonal: across becomes down, and its width its height.</summary>
    public Box<T> Turned() => new(Top, Left, Height, Width);

    /// <summary>A place along the box from its left: 0 its left edge, 1 its right.</summary>
    public T Across(T part) => Left + Width * part;

    /// <summary>A place down the box from its top: 0 its top edge, 1 its bottom.</summary>
    public T Down(T part) => Top + Height * part;

    /// <summary>The smallest box that holds this one and another.</summary>
    public Box<T> With(Box<T> other)
    {
        T left = T.Min(Left, other.Left), top = T.Min(Top, other.Top);
        T right = T.Max(Right, other.Right), bottom = T.Max(Bottom, other.Bottom);
        return new Box<T>(left, top, right - left, bottom - top);
    }

    double IBox.Left => double.CreateChecked(Left);

    double IBox.Top => double.CreateChecked(Top);

    double IBox.Width => double.CreateChecked(Width);

    double IBox.Height => double.CreateChecked(Height);

    double IBox.Right => double.CreateChecked(Right);

    double IBox.Bottom => double.CreateChecked(Bottom);

    IPoint IBox.Middle => Middle;

    IBox IBox.Grown(double by) => Grown(T.CreateChecked(by));

    IBox IBox.Moved(double across, double down) => Moved(T.CreateChecked(across), T.CreateChecked(down));

    IBox IBox.With(IBox other) => With(Ours(other));

    IBox IBox.Grown(double left, double top, double right, double bottom) => Grown(T.CreateChecked(left),
        T.CreateChecked(top), T.CreateChecked(right), T.CreateChecked(bottom));

    IBox IBox.OnLine(double y) => OnLine(T.CreateChecked(y));

    IBox IBox.Turned() => Turned();

    double IBox.Across(double part) => double.CreateChecked(Across(T.CreateChecked(part)));

    double IBox.Down(double part) => double.CreateChecked(Down(T.CreateChecked(part)));

    /// <summary>
    /// Another box as one of ours. It is one already when the picture is drawn by one geometry, and a box of
    /// another family is read through the seam.
    /// </summary>
    private static Box<T> Ours(IBox box) => box as Box<T> ?? new Box<T>(T.CreateChecked(box.Left),
        T.CreateChecked(box.Top), T.CreateChecked(box.Width), T.CreateChecked(box.Height));
}
