using LouisAgent.Choices;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Svg;

/// <summary>Draws any diagram with the renderer for its kind.</summary>
/// <remarks>
/// Strategy (Refactoring.Guru): each kind of diagram is drawn by its own <see cref="IDiagramRenderer"/>, picked here
/// by the kind the file names, so a new kind (the cards, then the sequences and the classes) is one new class and one
/// line in this list, never another case in a switch the others share.
/// </remarks>
public static class DiagramRenderers
{
    /// <summary>Every kind's renderer.</summary>
    private static readonly IReadOnlyList<IDiagramRenderer> All = [new FlowRenderer(), new CardsRenderer()];

    /// <summary>The diagram as one SVG file's text.</summary>
    public static string Render(DiagramSource source) =>
        (All.FirstOrDefault(renderer => renderer.Kind == source.Kind)
         ?? throw new NotSupportedException(
             DiagramText.Format(DiagramMessage.NoRenderer, new { Kind = source.Kind.Wire() })))
        .Render(source);
}
