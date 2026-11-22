using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Engine.Messages.Holes.Reading;
using LouisAgent.Diagrams.Engine.Svg.Fits;
using LouisAgent.Diagrams.Engine.Svg.Renderers;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Overview;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Structure;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Types;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg;

/// <summary>Draws any diagram with the renderer for its kind.</summary>
/// <remarks>
/// Strategy (Refactoring.Guru): each kind of diagram is drawn by its own <see cref="IDiagramRenderer"/>, picked here
/// by the kind the file names, so a new kind (the cards, then the sequences and the classes) is one new class and one
/// line in this list, never another case in a switch the others share. It is also the one place that says which
/// geometry a picture's boxes and points are made by: the one in use, unless a caller hands it another, so no
/// drawing picks a family for itself.
/// </remarks>
public static class DiagramRenderers
{
    /// <summary>Every kind's renderer.</summary>
    private static readonly IReadOnlyList<IDiagramRenderer> All =
    [
        new FlowRenderer(), new CardsRenderer(), new SequenceRenderer(), new ClassesRenderer(), new TicketRenderer(),
        new NestedRenderer(), new CompositeRenderer()
    ];

    /// <summary>
    /// The diagram as one SVG file's text, drawn with the geometry in use unless it is told another.
    /// </summary>
    public static string Render(DiagramSource source, IGeometry? geometry = null)
    {
        var renderer = All.FirstOrDefault(one => one.Kind == source.Kind);
        if (renderer is not null) return renderer.Render(source, geometry ?? Geometries.InUse);

        var noRenderer = new NoRendererHoles(source.Kind.Wire());
        throw new NotSupportedException(DiagramText.Format(DrawingMessage.NoRenderer, noRenderer));
    }

    /// <summary>
    /// The diagram as a page holds it inline (F127.4.1): the same drawing, its styles kept to itself
    /// (<see cref="PageSvg"/>), so the page's colour variables reach it and its rules reach nothing else. Asked for
    /// the narrow fit (F169.5), it draws the picture's narrow form, for a phone, when it has one.
    /// </summary>
    public static string RenderForPage(DiagramSource source, PictureFit fit = PictureFit.Wide)
    {
        var drawn = fit == PictureFit.Narrow ? NarrowFit.Of(source) ?? source : source;
        return PageSvg.Of(Render(drawn));
    }

    /// <summary>
    /// The diagram as the docs show it: drawn without its caption, then framed with the caption as a note bar
    /// under it (<see cref="TitledFrame"/>).
    /// </summary>
    public static string RenderTitled(DiagramSource source, IGeometry? geometry = null)
    {
        geometry ??= Geometries.InUse;
        return TitledFrame.Around(Render(source with { Caption = null }, geometry), source, geometry);
    }
}
