using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Web.Components.Graphs;

namespace LouisAgent.Web.Tests.Components.Graphs;

/// <summary>
/// F160.1: a page's picture is drawn by the diagram engine's own renderer, inline as SVG (Louis: "we must use
/// digrams to render the graphs in svg on the page"), wearing the page's theme and style, with its caption under it.
/// </summary>
public class DiagramFigureTests
{
    /// <summary>A small flow.</summary>
    private static DiagramSource Flow() => new(DiagramKind.Flow, "One turn", null,
        new GraphSpec([new GraphNode("host", "A host", null, 0), new GraphNode("model", "The model", null, 1)],
            [new GraphEdge("host", "model", "asks")]));

    /// <summary>The picture is the engine's SVG inline, wearing flat or glass, with its caption.</summary>
    [Test]
    public void ThePicture_IsTheEnginesSvg_WithItsCaption()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DiagramFigure>(figure => figure
            .Add(f => f.Source, Flow())
            .Add(f => f.Caption, "One turn through the framework"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("figure.diagram-figure svg").InnerHtml, Does.Contain("[data-theme=\"flat\"]"));
            Assert.That(cut.Find("figure.diagram-figure figcaption").TextContent.Trim(),
                Is.EqualTo("One turn through the framework"));
        });
    }

    /// <summary>With no caption, the figure draws no empty caption.</summary>
    [Test]
    public void WithNoCaption_ThereIsNone()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DiagramFigure>(figure => figure.Add(f => f.Source, Flow()));

        Assert.That(cut.FindAll("figcaption"), Is.Empty);
    }
}
