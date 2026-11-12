using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Blocks;

/// <summary>
/// Draws a card the agent wrote: the library draws the ticket as SVG for a page, and the chat holds it as it is.
/// </summary>
/// <remarks>
/// The library's markup, not a component's: the ticket is drawn once, in <c>louis-agent.diagrams</c>, for the docs
/// and the page alike, so the card in the chat is the card the tests' golden pins (ADR 0118).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public partial class DrawnCard
{
    /// <summary>The ticket to draw.</summary>
    [Parameter, EditorRequired]
    public DiagramSource Source { get; set; } = default!;

    /// <summary>The drawing, written by the library and encoded there, so it goes into the page as it is.</summary>
    private MarkupString Svg => new(DiagramRenderers.RenderForPage(Source));
}
