using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Overview;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

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
            Assert.That(drawn, Is.EqualTo(OverviewDrawing.Of(Overview, Geometries.InUse)));
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

    /// <summary>
    /// A card may wear a tone of its own (F162.1, Louis: "give each one a color this color we can then reuse talking
    /// about it"): a headline's badge, and a glass card's tint and icon, in that tone; a card that names none keeps
    /// the picture's violet.
    /// </summary>
    [Test]
    public void ACardsTone_ColoursItsBadgeTintAndIcon()
    {
        var teal = new DiagramSource(DiagramKind.Cards, "The web at a glance", Cards: new CardsSpec(
            [
                new CardRow([new Card("family", DiagramIcon.Browser, "The web", "src/web", "", CardLook.Highlight,
                    DiagramTone.Teal)]),
                new CardRow([new Card("one", DiagramIcon.Box, "One part", null, "What it does.", Tone: DiagramTone.Teal)]),
                // No line reaches it, so it is a quiet card, and its icon still wears its tone.
                new CardRow([new Card("two", DiagramIcon.Box, "Apart", null, "On its own.", Tone: DiagramTone.Red)])
            ],
            [new CardLink("family", "one")]));

        string drawn = DiagramRenderers.Render(teal);
        string plain = DiagramRenderers.Render(Overview);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Does.Contain("class=\"crd-badge tone-teal\"").And.Contain("class=\"crd-tint tone-teal\"")
                .And.Contain("class=\"crd-icon tone-teal\""));
            Assert.That(drawn, Does.Contain(".crd-tint.tone-teal { fill: var(--teal-line); }"));
            Assert.That(drawn, Does.Contain("class=\"crd-icon quiet tone-red\""), "a quiet card's icon in its tone");
            Assert.That(plain, Does.Contain("class=\"crd-tint\"").And.Not.Contain("class=\"crd-tint tone-"),
                "no tone, the violet");
        });
    }
}
