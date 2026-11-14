using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg;

/// <summary>Draws one kind of diagram as an SVG file's text.</summary>
public interface IDiagramRenderer
{
    /// <summary>The kind it draws.</summary>
    DiagramKind Kind { get; }

    /// <summary>
    /// The diagram as one SVG, its style holding both looks, its boxes and points made by the geometry it is
    /// handed.
    /// </summary>
    string Render(DiagramSource source, IGeometry geometry);
}
