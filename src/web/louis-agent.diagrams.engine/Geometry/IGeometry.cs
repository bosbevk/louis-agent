namespace LouisAgent.Diagrams.Engine.Geometry;

/// <summary>Makes the boxes and points a picture is laid out with, all of one family.</summary>
/// <remarks>
/// Abstract Factory (Gang of Four, Refactoring.Guru), Louis's pick of three for what the seam covers. There are
/// two families, ours and one on Microsoft's types, and a layout is handed one geometry and never names a
/// family. So a whole picture is drawn by one of them, and none mixes a box of one with a point of the other.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/abstract-factory">Abstract Factory (Refactoring.Guru)</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public interface IGeometry
{
    /// <summary>A box, from its left and top edges and its size.</summary>
    IBox Box(double x, double y, double width, double height);

    /// <summary>A point.</summary>
    IPoint Point(double x, double y);

    /// <summary>The smallest box that holds every one of these, as a group's frame holds its boxes.</summary>
    IBox Around(IEnumerable<IBox> boxes);
}
