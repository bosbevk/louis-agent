using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Structure;

/// <summary>
/// Draws a composite structure diagram (F134.13): the inside of one class, by handing it to its drawing.
/// </summary>
/// <remarks>
/// Strategy, as every kind's renderer is: one <see cref="IDiagramRenderer"/> a kind, picked by
/// <see cref="DiagramRenderers"/>. It stands beside its drawing, since the renderers' own folder is full.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/strategy">Strategy (Refactoring.Guru)</seealso>
internal sealed class CompositeRenderer : IDiagramRenderer
{
    /// <summary>This is the composite structure diagram's.</summary>
    public DiagramKind Kind => DiagramKind.Composite;

    /// <summary>The picture as an SVG, drawn from what the flow's layout placed.</summary>
    public string Render(DiagramSource source, IGeometry geometry) => CompositeDrawing.Of(source, geometry);
}
