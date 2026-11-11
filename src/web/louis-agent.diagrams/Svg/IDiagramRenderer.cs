using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Svg;

/// <summary>Draws one kind of diagram as an SVG file's text.</summary>
public interface IDiagramRenderer
{
    /// <summary>The kind it draws.</summary>
    DiagramKind Kind { get; }

    /// <summary>The diagram as one SVG, its style holding both looks.</summary>
    string Render(DiagramSource source);
}
