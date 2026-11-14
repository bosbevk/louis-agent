using LouisAgent.Diagrams.Classes;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Renderers.Parts;

namespace LouisAgent.Diagrams.Svg.Renderers;

/// <summary>
/// Draws a class chart (CL60.4): turned into a flow by <see cref="ClassFlow"/>, then drawn as every flow was before
/// F134.8, by <see cref="PlainFlowDrawing"/>, so a type looks like a box with its members and a relation like an
/// arrow. It keeps that drawing while the flow's own renderer changes.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data</seealso>
internal sealed class ClassesRenderer : IDiagramRenderer
{
    /// <summary>This is the class chart's.</summary>
    public DiagramKind Kind => DiagramKind.Classes;

    /// <summary>The class chart as one SVG file's text.</summary>
    public string Render(DiagramSource source)
    {
        var asFlow = source with
        {
            Kind = DiagramKind.Flow,
            Flow = ClassFlow.Of(source.Classes!),
            Classes = null
        };
        return PlainFlowDrawing.Of(asFlow);
    }
}
