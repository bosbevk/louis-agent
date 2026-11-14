using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Overview;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Overview;

/// <summary>
/// A cards picture is drawn in the new look (F134.9): the docs' tool and the chat both draw one through the kind's
/// one renderer, so both get the headline, the rail and the glass.
/// </summary>
public class CardsRendererTests
{
    /// <summary>A project and its two parts.</summary>
    private static readonly DiagramSource Overview = new(DiagramKind.Cards, "A project at a glance",
        "One project, two parts.",
        Cards: new CardsSpec(
            [
                new CardRow([
                    new Card("project", DiagramIcon.Chip, "The project", "a.project", "What it is.",
                        CardLook.Highlight)
                ]),
                new CardRow([
                    new Card("one", DiagramIcon.Box, "One part", null, "What it does."),
                    new Card("two", DiagramIcon.Box, "Another part", null, "What it does.")
                ])
            ],
            [new CardLink("project", "one"), new CardLink("project", "two")]));

    /// <summary>
    /// The renderer hands the picture to the new drawing: a headline, glass cards on a rail, the picture's own
    /// title, and none of the old frame and boxes.
    /// </summary>
    [Test]
    public void ACardsPicture_IsDrawnInTheNewLook()
    {
        string drawn = DiagramRenderers.Render(Overview);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.EqualTo(OverviewDrawing.Of(Overview)));
            Assert.That(drawn, Does.Contain("class=\"mrq-title\"").And.Contain("crd-headline").And.Contain("crd-rail"));
            Assert.That(drawn, Does.Not.Contain("class=\"frame\"").And.Not.Contain("class=\"card"));
        });
    }

    /// <summary>
    /// The chat's copy is the same drawing, kept to itself, and it keeps the look's paints, since its shapes are
    /// filled with them.
    /// </summary>
    [Test]
    public void TheChatsCopy_IsTheSameDrawing_WithItsPaints()
    {
        string page = DiagramRenderers.RenderForPage(Overview);

        Assert.That(page, Does.Contain("crd-headline").And.Contain("<radialGradient id=\"mrq-light\"").And
            .Contain("@scope"));
    }
}
