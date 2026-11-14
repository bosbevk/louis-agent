using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Types;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Types;

/// <summary>
/// Draws a class chart, a project's interface design, in the cards' look (F134.11), <see cref="ClassesDrawing"/>:
/// each type a glass card with a badge that says what it is, each link a line whose colour and ends say its kind,
/// and the picture's own key under it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data</seealso>
internal sealed class ClassesRenderer : IDiagramRenderer
{
    /// <summary>This is the class chart's.</summary>
    public DiagramKind Kind => DiagramKind.Classes;

    /// <summary>The class chart as one SVG file's text.</summary>
    public string Render(DiagramSource source) => ClassesDrawing.Of(source);
}
