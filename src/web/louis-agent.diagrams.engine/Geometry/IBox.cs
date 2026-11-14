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

    /// <summary>The box with room added on every side.</summary>
    IBox Grown(double by);

    /// <summary>The same box, somewhere else.</summary>
    IBox Moved(double across, double down);

    /// <summary>The smallest box that holds this one and another.</summary>
    IBox With(IBox other);
}
