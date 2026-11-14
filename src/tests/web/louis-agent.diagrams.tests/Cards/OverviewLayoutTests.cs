using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Cards.Placed;
using LouisAgent.Diagrams.Cards.Rails;
using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Diagrams.Tests.Cards;

/// <summary>
/// An overview placed in the new look (F134.9): rows of cards under one another, a row's cards the same size, a
/// gap as tall as its rails need, and each rail's ends on the edges of its cards.
/// </summary>
public class OverviewLayoutTests
{
    /// <summary>A plain card, with a few words.</summary>
    private static Card Plain(string id) => new(id, DiagramIcon.Box, id, null, "What it is.");

    /// <summary>A highlighted card with a code name: a headline 170 tall.</summary>
    private static Card Headline(string id) =>
        new(id, DiagramIcon.Chip, "The project", "louis-agent.project", "What it is.", CardLook.Highlight);

    /// <summary>
    /// The usual shape: a headline that feeds a row of three, and under them a row of two that no link touches.
    /// </summary>
    private static CardsSpec Usual(CardLinkKind kind = CardLinkKind.Down) => new(
        [
            new CardRow([Headline("top")]),
            new CardRow([Plain("a"), Plain("b"), Plain("c")]),
            new CardRow([Plain("loose"), Plain("spare")])
        ],
        [new CardLink("top", "a", kind), new CardLink("top", "b", kind), new CardLink("top", "c", kind)]);

    private static PlacedCard Card(PlacedOverview placed, string id) => placed.Cards.Single(card => card.Card.Id == id);

    /// <summary>A row's cards share its width, a gap between them, and stand as tall as its tallest.</summary>
    [Test]
    public void ARowsCards_ShareItsWidth_AndItsTallestHeight()
    {
        var placed = OverviewLayout.Place(Usual());
        var row = new[] { Card(placed, "a"), Card(placed, "b"), Card(placed, "c") };

        Assert.Multiple(() =>
        {
            Assert.That(row.Select(card => card.W), Is.All.EqualTo(284));
            Assert.That(row.Select(card => card.X), Is.EqualTo(new[] { 0d, 306, 612 }));
            Assert.That(row.Select(card => card.H).Distinct().Count(), Is.EqualTo(1));
            Assert.That(row.Select(card => card.Y).Distinct().Count(), Is.EqualTo(1));
            Assert.That(Card(placed, "top").W, Is.EqualTo(896), "a card alone has the whole width");
        });
    }

    /// <summary>
    /// A gap a rail crosses is as tall as the rail needs, the rail a stem under the row above. A gap no link
    /// crosses is the plain gap. The picture ends at its last row's bottom.
    /// </summary>
    [Test]
    public void AGap_IsAsTallAsItsRailNeeds_OrThePlainGap()
    {
        var placed = OverviewLayout.Place(Usual());
        var (top, part, loose) = (Card(placed, "top"), Card(placed, "a"), Card(placed, "loose"));

        Assert.Multiple(() =>
        {
            Assert.That(top.Y, Is.Zero);
            Assert.That(placed.Rails.Single().Y, Is.EqualTo(top.H + RailPlaces.Stem));
            Assert.That(part.Y, Is.EqualTo(top.H + RailPlaces.Stem + RailPlaces.Drop));
            Assert.That(loose.Y, Is.EqualTo(part.Y + part.H + OverviewLayout.RowGap));
            Assert.That((placed.Width, placed.Height), Is.EqualTo((896d, loose.Y + loose.H)));
        });
    }

    /// <summary>
    /// A rail's ends stand on its cards' edges: under the card above it, at its middle and a little clear of it
    /// since it is a headline, and on the top of each card below.
    /// </summary>
    [Test]
    public void ARailsEnds_StandOnItsCardsEdges()
    {
        var placed = OverviewLayout.Place(Usual());
        var rail = placed.Rails.Single();
        var above = rail.Ends.Single(end => end.Above);

        Assert.Multiple(() =>
        {
            Assert.That((above.Card, above.X, above.Y),
                Is.EqualTo(("top", 448d, Card(placed, "top").H + OverviewLayout.HeadlineClear)));
            Assert.That(rail.Ends.Where(end => !end.Above).Select(end => (end.Card, end.X, end.Y)), Is.EqualTo(
                new[] { "a", "b", "c" }.Select(id => (id, Card(placed, id).X + 142, Card(placed, id).Y))));
            Assert.That((rail.Left, rail.Right), Is.EqualTo((142d, 754d)));
        });
    }

    /// <summary>
    /// A drop into a headline ends a little above it, so its arrowhead stands clear of the badge. A stem under a
    /// plain card starts on its bottom edge.
    /// </summary>
    [Test]
    public void ADropIntoAHeadline_EndsALittleAboveIt()
    {
        var spec = new CardsSpec(
            [new CardRow([Plain("a"), Plain("b")]), new CardRow([Headline("engine")])],
            [new CardLink("a", "engine"), new CardLink("b", "engine")]);

        var placed = OverviewLayout.Place(spec);

        Assert.Multiple(() =>
        {
            Assert.That(placed.Rails.Single().Ends.Single(end => !end.Above).Y,
                Is.EqualTo(Card(placed, "engine").Y - OverviewLayout.HeadlineClear));
            Assert.That(placed.Rails.Single().Ends.Where(end => end.Above).Select(end => end.Y),
                Is.All.EqualTo(Card(placed, "a").H));
        });
    }

    /// <summary>An end has an arrowhead where a link points at its card.</summary>
    [TestCase(CardLinkKind.Down, false, true)]
    [TestCase(CardLinkKind.Up, true, false)]
    [TestCase(CardLinkKind.Both, true, true)]
    public void AnEnd_HasAnArrowhead_WhereALinkPointsAtItsCard(CardLinkKind kind, bool above, bool below)
    {
        var rail = OverviewLayout.Place(Usual(kind)).Rails.Single();

        Assert.Multiple(() =>
        {
            Assert.That(rail.Ends.Where(end => end.Above).Select(end => end.Arrow), Is.All.EqualTo(above));
            Assert.That(rail.Ends.Where(end => !end.Above).Select(end => end.Arrow), Is.All.EqualTo(below));
        });
    }

    /// <summary>
    /// A card no link touches is quiet, and so lower, in a picture that has links. In a picture with none, no card
    /// is more loose than another, and none is quiet.
    /// </summary>
    [Test]
    public void ACardNoLinkTouches_IsQuiet_OnlyInAPictureWithLinks()
    {
        var linked = OverviewLayout.Place(Usual());
        var bare = Usual() with { Links = [] };
        var unlinked = OverviewLayout.Place(bare);

        Assert.Multiple(() =>
        {
            Assert.That(linked.Cards.Where(card => card.Quiet).Select(card => card.Card.Id),
                Is.EqualTo(new[] { "loose", "spare" }));
            Assert.That(Card(linked, "loose").H, Is.LessThan(Card(linked, "a").H));
            Assert.That(unlinked.Cards.Select(card => card.Quiet), Is.All.False);
            Assert.That(unlinked.Rails, Is.Empty);
        });
    }

    /// <summary>
    /// Two rails that would lie on one another make their gap a step taller, the second rail a step under the
    /// first.
    /// </summary>
    [Test]
    public void TwoRailsThatOverlap_MakeTheirGapAStepTaller()
    {
        var spec = new CardsSpec(
            [new CardRow([Plain("one"), Plain("two")]), new CardRow([Plain("a"), Plain("b")])],
            [new CardLink("one", "a"), new CardLink("one", "b"), new CardLink("two", "b")]);

        var placed = OverviewLayout.Place(spec);
        double bottom = Card(placed, "one").H;

        Assert.Multiple(() =>
        {
            Assert.That(placed.Rails.Select(rail => rail.Y),
                Is.EqualTo(new[] { bottom + RailPlaces.Stem, bottom + RailPlaces.Stem + RailPlaces.Step }));
            Assert.That(Card(placed, "a").Y, Is.EqualTo(bottom + RailPlaces.Stem + RailPlaces.Step + RailPlaces.Drop));
        });
    }

    /// <summary>No card overlaps another, and every card stands inside the picture.</summary>
    [Test]
    public void TheCards_DontOverlap_AndStayInsideThePicture()
    {
        var placed = OverviewLayout.Place(Usual());
        var cards = placed.Cards;

        Assert.Multiple(() =>
        {
            for (int a = 0; a < cards.Count; a++)
                for (int b = a + 1; b < cards.Count; b++)
                    Assert.That(cards[a].X + cards[a].W <= cards[b].X || cards[b].X + cards[b].W <= cards[a].X
                                || cards[a].Y + cards[a].H <= cards[b].Y || cards[b].Y + cards[b].H <= cards[a].Y,
                        $"{cards[a].Card.Id} and {cards[b].Card.Id}");
            Assert.That(cards.Max(card => card.X + card.W), Is.LessThanOrEqualTo(placed.Width));
            Assert.That(cards.Max(card => card.Y + card.H), Is.LessThanOrEqualTo(placed.Height));
        });
    }
}
