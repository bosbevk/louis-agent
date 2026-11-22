using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Fits;
using LouisAgent.Diagrams.Sources;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Graphs;

/// <summary>
/// A page's picture (F164.1, Louis: "we must use digrams to render the graphs in svg on the page"): any kind the
/// diagram engine draws, drawn by its own renderer as the docs' pictures are, inline on the page.
/// </summary>
/// <remarks>
/// The engine's page copy, not a component's own drawing (ADR 0118), as the chat's ticket is: inline, its styles kept
/// to it, so it wears the page's theme and style by the engine's palette for each (Louis's pick C). A kind the page
/// needs that the engine lacks is added to the engine, never drawn here. On a phone (F169.5, Louis's pick "A: both in
/// the page, CSS shows one", over a script that swaps them and over two files in a <c>picture</c> tag) it holds the
/// picture's narrow drawing beside its wide one, and Bootstrap's display classes show one: right from the first
/// paint and with no script, paid for in bytes, since the picture is in the page twice.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public partial class DiagramFigure
{
    /// <summary>The picture, as the engine reads it.</summary>
    [Parameter, EditorRequired]
    public DiagramSource Source { get; set; } = default!;

    /// <summary>A line under the picture saying what it shows, when it needs one.</summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>The drawing, written by the engine and encoded there, so it goes into the page as it is.</summary>
    private MarkupString Svg => new(DiagramRenderers.RenderForPage(Source));

    /// <summary>Whether the engine can draw this picture for a phone (F169.5); the figure asks, and never guesses.</summary>
    private bool HasNarrow => NarrowFit.Has(Source);

    /// <summary>The drawing for a phone, the picture's narrow form, as the engine draws it.</summary>
    private MarkupString NarrowSvg => new(DiagramRenderers.RenderForPage(Source, PictureFit.Narrow));
}
