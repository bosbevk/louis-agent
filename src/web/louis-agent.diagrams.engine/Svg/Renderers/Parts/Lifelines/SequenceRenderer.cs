using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// Draws a sequence, who calls whom in order, in the cards' look (F134.10), <see cref="SequenceDrawing"/>: each
/// participant a badge in its tone with its name, a call a beam from violet to blue, an answer a quiet dashed
/// line, and the picture's own key under it.
/// </summary>
internal sealed class SequenceRenderer : IDiagramRenderer
{
    public DiagramKind Kind => DiagramKind.Sequence;

    public string Render(DiagramSource source) => SequenceDrawing.Of(source);
}
