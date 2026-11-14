using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Renderers.Parts.Overview;

namespace LouisAgent.Diagrams.Svg.Renderers;

/// <summary>
/// Draws a cards picture, the overview a project doc opens with, in the new look (F134.9),
/// <see cref="OverviewDrawing"/>: the project as a headline on a light, and its parts on glass cards hung from a
/// lit rail.
/// </summary>
internal sealed class CardsRenderer : IDiagramRenderer
{
    public DiagramKind Kind => DiagramKind.Cards;

    public string Render(DiagramSource source) => OverviewDrawing.Of(source);
}
