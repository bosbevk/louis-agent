using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Web.Components.Graphs;

namespace LouisAgent.Web.Tests.Components.Graphs;

/// <summary>
/// F164.1: a page's picture is drawn by the diagram engine's own renderer, inline as SVG (Louis: "we must use
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

    /// <summary>A headline and the two cards it feeds, a picture with a narrow form.</summary>
    private static DiagramSource Cards() => new(DiagramKind.Cards, "At a glance", Cards: new CardsSpec(
        [
            new CardRow([new Card("one", DiagramIcon.Chip, "One", null, "", CardLook.Highlight)]),
            new CardRow([
                new Card("a", DiagramIcon.Box, "A", null, "a."), new Card("b", DiagramIcon.Box, "B", null, "b.")
            ])
        ],
        [new CardLink("one", "a"), new CardLink("one", "b")]));

    /// <summary>
    /// F169.5 (Louis's pick "A: both in the page, CSS shows one"): a picture with a narrow form is held twice, the
    /// narrow drawing shown below Bootstrap's medium screen and the wide one from it, under one caption.
    /// </summary>
    [Test]
    public void APictureWithANarrowForm_IsHeldTwice_UnderOneCaption()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DiagramFigure>(figure => figure
            .Add(f => f.Source, Cards())
            .Add(f => f.Caption, "One and its two"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("figure.diagram-figure svg"), Has.Count.EqualTo(2));
            Assert.That(cut.Find(".diagram-narrow.d-md-none svg").GetAttribute("width"), Is.EqualTo("342"));
            Assert.That(cut.Find(".diagram-wide.d-none.d-md-block svg").GetAttribute("width"),
                Is.Not.EqualTo("342"));
            Assert.That(cut.FindAll("figcaption"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// F169.5: the narrow drawing is shown at its own size, never stretched to its column, so its words are the
    /// engine's sizes; it shrinks only in a column narrower than it.
    /// </summary>
    [Test]
    public void TheNarrowDrawing_IsShownAtItsOwnSize()
    {
        string rules = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Graphs/DiagramFigure.razor.scss")).ReplaceLineEndings("\n");

        Assert.That(rules, Does.Contain(".diagram-narrow ::deep svg {\n    width: auto;\n    max-width: 100%;\n"));
    }

    /// <summary>A short sequence, a kind with no narrow form yet.</summary>
    private static DiagramSource Sequence() => DiagramReader.Read("""
        {
          "kind": "sequence", "title": "Short",
          "sequence": {
            "participants": [ { "id": "a", "label": "A" }, { "id": "b", "label": "B" } ],
            "steps": [ { "step": "message", "from": "a", "to": "b", "text": "ask" } ]
          }
        }
        """);

    /// <summary>
    /// F169.5: a picture with no narrow form is held once, in a frame that says it slides, which the keyboard can
    /// reach and scroll, named for the picture.
    /// </summary>
    [Test]
    public void APictureWithNoNarrowForm_IsHeldOnce_InAFrameThatSlides()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DiagramFigure>(figure => figure.Add(f => f.Source, Sequence()));
        var frame = cut.Find("figure.diagram-figure .diagram-slides");

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("figure.diagram-figure svg"), Has.Count.EqualTo(1));
            Assert.That(frame.QuerySelector("svg"), Is.Not.Null);
            Assert.That(frame.GetAttribute("tabindex"), Is.EqualTo("0"));
            Assert.That(frame.GetAttribute("role"), Is.EqualTo("region"));
            Assert.That(frame.GetAttribute("aria-label"), Is.EqualTo("Short"));
        });
    }

    /// <summary>
    /// F169.5: below Bootstrap's medium screen a sliding figure keeps its drawing at its own width, in a frame that
    /// scrolls sideways with a slim bar under it, so the page itself never grows wider than the screen.
    /// </summary>
    [Test]
    public void BelowMedium_ASlidingFigure_KeepsItsWidth_AndScrolls()
    {
        string rules = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Graphs/DiagramFigure.razor.scss")).ReplaceLineEndings("\n");
        int phoneAt = rules.IndexOf("@media (max-width: 767.98px)", StringComparison.Ordinal);

        Assert.That(phoneAt, Is.GreaterThan(0), "a rule below Bootstrap's medium screen");
        string phone = rules[phoneAt..];
        Assert.Multiple(() =>
        {
            Assert.That(phone, Does.Contain("    .diagram-slides {\n        overflow-x: auto;\n"));
            Assert.That(phone, Does.Contain("    .diagram-slides ::deep svg {\n        width: auto;\n" +
                                            "        max-width: none;\n"));
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
