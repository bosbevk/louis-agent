using LouisAgent.Diagrams.Classes;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Svg.Renderers;

/// <summary>
/// Draws a class chart (CL60.4): turned into a flow by <see cref="ClassFlow"/>, then drawn by the flows' renderer, so
/// a type looks like a flow's box with its members and a relation like its arrow.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data</seealso>
internal sealed class ClassesRenderer : IDiagramRenderer
{
    public DiagramKind Kind => DiagramKind.Classes;

    public string Render(DiagramSource source) =>
        new FlowRenderer().Render(source with
        {
            Kind = DiagramKind.Flow,
            Flow = ClassFlow.Of(source.Classes!),
            Classes = null
        });
}
