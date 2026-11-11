using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Placed;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;

namespace LouisAgent.Diagrams.Tests.Cards;

/// <summary>
/// An overview picture in the card style of Louis's figure (CL60.1 step 5): rows of cards in one rounded frame, a
/// row's cards the same size, every word inside its card, and a link from a card's bottom to the next row's top.
/// </summary>
public class CardLayoutTests
{
    /// <summary>Louis's figure: the engine on top, the identities and the packs under it, linked both ways.</summary>
    private static CardsSpec Figure() => new(
        [
            new CardRow([
                new Card("engine", DiagramIcon.Chip, "Engine", "louis-agent.core",
                    "The runtime, tool loop, model integration, skills, and shared agent behaviour.")
            ]),
            new CardRow([
                new Card("identities", DiagramIcon.User, "Identities", null,
                    "Who the agent is: voice, modes, roles, and specialist routing."),
                new Card("packs", DiagramIcon.Box, "Packs", null,
                    "What the agent can do: tools, guidance, configuration, and capabilities.")
            ])
        ],
        [
            new CardLink("engine", "identities", CardLinkKind.Both),
            new CardLink("engine", "packs", CardLinkKind.Both)
        ]);

    /// <summary>Louis's figure, drawn, matches its golden file.</summary>
    [Test]
    public void LouissFigure_MatchesTheGoldenFile() =>
        Golden.Match("cards.svg", DiagramRenderers.Render(new DiagramSource(DiagramKind.Cards,
            "Engine, identities and packs", "The separation ADR 0027 and ADR 0028 document.", Cards: Figure())));

    /// <summary>A row's cards are the same width and the same height, and side by side.</summary>
    [Test]
    public void ARowsCards_AreTheSameSize()
    {
        var placed = CardLayout.Place(Figure());

        var row = placed.Cards.Where(card => card.Card.Id != "engine").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(row[0].W, Is.EqualTo(row[1].W));
            Assert.That(row[0].H, Is.EqualTo(row[1].H));
            Assert.That(row[0].Y, Is.EqualTo(row[1].Y));
            Assert.That(row[1].X, Is.GreaterThan(row[0].X + row[0].W), "a gap between them");
        });
    }

    /// <summary>No card overlaps another, and every card stands inside the frame.</summary>
    [Test]
    public void TheCards_DontOverlap_AndStayInsideTheFrame()
    {
        var placed = CardLayout.Place(Figure());

        Assert.Multiple(() =>
        {
            foreach (var card in placed.Cards)
            {
                Assert.That(card.X, Is.GreaterThan(0), card.Card.Id);
                Assert.That(card.Y, Is.GreaterThan(0), card.Card.Id);
                Assert.That(card.X + card.W, Is.LessThan(placed.Width), card.Card.Id);
                Assert.That(card.Y + card.H, Is.LessThan(placed.Height), card.Card.Id);
                foreach (var other in placed.Cards.Where(other => other != card))
                    Assert.That(Overlap(card, other), Is.False, $"{card.Card.Id} and {other.Card.Id}");
            }
        });
    }

    /// <summary>Every line of a card's words fits inside it, its padding kept, at the code font's width.</summary>
    [Test]
    public void EveryLine_FitsInsideItsCard()
    {
        var placed = CardLayout.Place(Figure());

        Assert.Multiple(() =>
        {
            foreach (var card in placed.Cards)
                Assert.That(card.TextLines.Max(line => line.Length) * CardLayout.TextCharWidth,
                    Is.LessThanOrEqualTo(card.W - 2 * CardLayout.CardPad), card.Card.Id);
        });
    }

    /// <summary>A link runs from the upper card's bottom middle to the lower card's top middle.</summary>
    [Test]
    public void ALink_RunsFromTheUpperCardsBottom_ToTheLowerCardsTop()
    {
        var placed = CardLayout.Place(Figure());

        var engine = placed.Cards.Single(card => card.Card.Id == "engine");
        var packs = placed.Cards.Single(card => card.Card.Id == "packs");
        var link = placed.Links.Single(link => link.Link.To == "packs");
        Assert.Multiple(() =>
        {
            Assert.That(link.Path, Does.StartWith($"M {engine.X + engine.W / 2:0.#} {engine.Y + engine.H:0.#} "));
            Assert.That(link.Path, Does.EndWith($"{packs.X + packs.W / 2:0.#} {packs.Y:0.#}"));
        });
    }

    /// <summary>A card whose title and code chip don't fit one line puts the chip under the title.</summary>
    [Test]
    public void ALongTitleAndChip_PutTheChipUnderTheTitle()
    {
        var spec = new CardsSpec(
            [
                new CardRow([
                    new Card("a", DiagramIcon.Box, "A rather long title for a card", "louis-agent.something.long",
                        "Words."),
                    new Card("b", DiagramIcon.Box, "B", null, "Words."),
                    new Card("c", DiagramIcon.Box, "C", null, "Words.")
                ])
            ],
            []);

        var card = CardLayout.Place(spec).Cards[0];

        Assert.That(card.ChipBelow, Is.True);
    }

    /// <summary>Whether two placed cards share any ground.</summary>
    private static bool Overlap(PlacedCard a, PlacedCard b) =>
        a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H;
}
