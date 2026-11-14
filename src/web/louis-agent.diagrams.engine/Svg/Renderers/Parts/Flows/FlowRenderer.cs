using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;

/// <summary>
/// Draws a flow in the new look (F134.8), <see cref="FlowDrawing"/>: its own title, boxes level with what feeds
/// them, each arrow at its own point, and a key.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
internal sealed class FlowRenderer : IDiagramRenderer
{
    /// <summary>This is the flow's.</summary>
    public DiagramKind Kind => DiagramKind.Flow;

    /// <summary>The flow as one SVG file's text.</summary>
    public string Render(DiagramSource source) => FlowDrawing.Of(source, Geometries.InUse);
}
