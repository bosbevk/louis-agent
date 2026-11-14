namespace LouisAgent.Diagrams.Engine.Geometry;

/// <summary>
/// A box in a picture, which knows its own sums: where its edges and its middle are, and what it is when it is
/// grown, moved, or joined with another.
/// </summary>
/// <remarks>
/// Value Object (Fowler), as the cure for Primitive Obsession: a box was four bare numbers, and every layout
/// and drawing worked its right edge out again where it stood. A box that is grown, moved or joined is a new
/// box of the same family, so a picture drawn from one geometry never holds a box of another.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public interface IBox
{
    /// <summary>Its left edge.</summary>
    double Left { get; }

    /// <summary>Its top edge.</summary>
    double Top { get; }

    /// <summary>How wide it is.</summary>
    double Width { get; }

    /// <summary>How tall it is.</summary>
    double Height { get; }

    /// <summary>Its right edge.</summary>
    double Right { get; }

    /// <summary>Its bottom edge.</summary>
    double Bottom { get; }

    /// <summary>The point in its middle.</summary>
    IPoint Middle { get; }

    /// <summary>Half its height: how far its middle is from its top and from its bottom.</summary>
    double HalfHeight { get; }

    /// <summary>The box with room added on every side.</summary>
    IBox Grown(double by);

    /// <summary>
    /// The box with room added on each side, its own on every one, as a frame has more room over its label.
    /// </summary>
    IBox Grown(double left, double top, double right, double bottom);

    /// <summary>The same box with its middle on a line across the picture, at this height.</summary>
    IBox OnLine(double y);

    /// <summary>The box turned over its diagonal: across becomes down, and its width its height.</summary>
    IBox Turned();

    /// <summary>A place along the box from its left: 0 its left edge, 1 its right, 0.5 its middle.</summary>
    double Across(double part);

    /// <summary>A place down the box from its top: 0 its top edge, 1 its bottom.</summary>
    double Down(double part);

    /// <summary>The same box, somewhere else.</summary>
    IBox Moved(double across, double down);

    /// <summary>The smallest box that holds this one and another.</summary>
    IBox With(IBox other);

    /// <summary>Whether another box lies within this one, or is as big as it.</summary>
    bool Holds(IBox other);

    /// <summary>
    /// Whether a level or upright stretch between two points passes through this box, that far inside its edges:
    /// a stretch that only touches an edge passes through when it is asked with nothing.
    /// </summary>
    bool CrossedBy(IPoint from, IPoint to, double inside);
}
