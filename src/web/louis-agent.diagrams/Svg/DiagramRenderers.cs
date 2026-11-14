using LouisAgent.Choices;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Messages.Holes.Reading;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Renderers;
using LouisAgent.Diagrams.Svg.Renderers.Parts.Structure;

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
    private static readonly IReadOnlyList<IDiagramRenderer> All =
    [
        new FlowRenderer(), new CardsRenderer(), new SequenceRenderer(), new ClassesRenderer(), new TicketRenderer(),
        new ComponentsRenderer(), new CompositeRenderer()
    ];

    /// <summary>The diagram as one SVG file's text.</summary>
    public static string Render(DiagramSource source)
    {
        var renderer = All.FirstOrDefault(one => one.Kind == source.Kind);
        if (renderer is not null) return renderer.Render(source);

        var noRenderer = new NoRendererHoles(source.Kind.Wire());
        throw new NotSupportedException(DiagramText.Format(DrawingMessage.NoRenderer, noRenderer));
    }

    /// <summary>
    /// The diagram as a page holds it inline (F127.4.1): the same drawing, its styles kept to itself
    /// (<see cref="PageSvg"/>), so the page's colour variables reach it and its rules reach nothing else.
    /// </summary>
    public static string RenderForPage(DiagramSource source) => PageSvg.Of(Render(source));

    /// <summary>
    /// The diagram as the docs show it: drawn without its caption, then framed with its title bar on top and the
    /// caption as a note bar under it (<see cref="TitledFrame"/>).
    /// </summary>
    public static string RenderTitled(DiagramSource source) =>
        TitledFrame.Around(Render(source with { Caption = null }), source);
}
