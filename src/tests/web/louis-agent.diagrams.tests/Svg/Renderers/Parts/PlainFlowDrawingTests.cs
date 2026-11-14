using System.Text.RegularExpressions;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Renderers.Parts;
using LouisAgent.Diagrams.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts;

/// <summary>
/// The plain drawing as one SVG file (CL60.1 step 3), which every flow had before F134.8 and a class chart keeps:
/// the site's boxes and arrows by ADR 0103's rules, in the site's palette, dark or light as the reader's screen
/// is, with nothing loaded from outside the file.
/// </summary>
public class PlainFlowDrawingTests
{
    /// <summary>
    /// The site's Turn diagram in small: a host, the engine with a note, the model, and words on arrows.
    /// </summary>
    private static DiagramSource Turn() => new(DiagramKind.Flow, "One turn", "From a host to the model",
        new GraphSpec(
            [
                new GraphNode("host", "A host", null, 0),
                new GraphNode("engine", "The engine", "the loop, the guards, the ledger", 1),
                new GraphNode("model", "The model", null, 2)
            ],
            [new GraphEdge("host", "engine", "a turn"), new GraphEdge("engine", "model", "asks")]));

    /// <summary>The drawing matches its golden file.</summary>
    [Test]
    public void AFlow_MatchesTheGoldenFile() =>
        Golden.Match("flow.svg", PlainFlowDrawing.Of(Turn()));

    /// <summary>One file holds both looks: light by default, dark when the reader's screen is.</summary>
    [Test]
    public void TheStyle_HoldsTheLightAndTheDarkPalette()
    {
        string svg = PlainFlowDrawing.Of(Turn());

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("@media (prefers-color-scheme: dark)"));
            Assert.That(svg, Does.Contain(SvgTheme.Site.Light.Ground).And.Contain(SvgTheme.Site.Dark.Ground));
        });
    }

    /// <summary>Nothing reaches outside the file, so GitHub and Rider show it as it is.</summary>
    [Test]
    public void Nothing_LeavesTheFile()
    {
        string svg = PlainFlowDrawing.Of(Turn());

        Assert.That(svg, Does.Not.Contain("href=\"http").And.Not.Contain("url(http").And.Not.Contain("@import")
            .And.Not.Contain("<image"));
    }

    /// <summary>Every box, arrow and word is drawn, each box its label and its note's lines.</summary>
    [Test]
    public void EveryBoxArrowAndWord_IsDrawn()
    {
        string svg = PlainFlowDrawing.Of(Turn());

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(svg, "class=\"box\""), Has.Count.EqualTo(3));
            Assert.That(Regex.Matches(svg, "class=\"edge\""), Has.Count.EqualTo(2));
            Assert.That(svg, Does.Contain(">a turn<").And.Contain(">asks<"));
            Assert.That(svg, Does.Contain(">The engine<").And.Contain(">the loop, the guards, the<"));
        });
    }

    /// <summary>The title is what a screen reader says, and the caption is drawn under the diagram.</summary>
    [Test]
    public void TheTitle_IsSaid_AndTheCaptionDrawn()
    {
        string svg = PlainFlowDrawing.Of(Turn());

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("aria-label=\"One turn\"").And.Contain("<title>One turn</title>"));
            Assert.That(svg, Does.Contain(">From a host to the model<"));
        });
    }
}
