using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Engine.Cards.Rails;
using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Diagrams.Tests.Cards.Rails;

/// <summary>
/// A cards picture's links become rails (F134.9): in the gap between two rows, the cards above that are linked to
/// the same cards below share one rail, and every other card above has a rail of its own.
/// </summary>
public class CardRailsTests
{
    /// <summary>Rows of cards named by their ids, top to bottom, and the links between them.</summary>
    private static CardsSpec Spec(string[][] rows, params CardLink[] links) => new(
        [.. rows.Select(row => new CardRow([.. row.Select(id => new Card(id, DiagramIcon.Box, id, null, id))]))],
        links);

    /// <summary>One card that feeds a whole row has one rail, with its three cards in the row's order.</summary>
    [Test]
    public void OneCardFeedingThree_IsOneRail()
    {
        var spec = Spec([["project"], ["chat", "admin", "site"]],
            new CardLink("project", "site"), new CardLink("project", "chat"), new CardLink("project", "admin"));

        var rails = CardRails.Of(spec);

        Assert.That(rails, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rails[0].Gap, Is.Zero, "the gap under the first row");
            Assert.That(rails[0].Above, Is.EqualTo(new[] { "project" }));
            Assert.That(rails[0].Below, Is.EqualTo(new[] { "chat", "admin", "site" }), "in the row's order");
            Assert.That(rails[0].Links, Has.Count.EqualTo(3));
        });
    }

    /// <summary>Two cards that feed the same card share a rail, as identity and packs do into the engine.</summary>
    [Test]
    public void TwoCardsFeedingOne_ShareARail()
    {
        var spec = Spec([["app"], ["identity", "packs"], ["engine"]],
            new CardLink("packs", "engine"), new CardLink("identity", "engine"));

        var rails = CardRails.Of(spec);

        Assert.That(rails, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rails[0].Gap, Is.EqualTo(1));
            Assert.That(rails[0].Above, Is.EqualTo(new[] { "identity", "packs" }), "in the row's order");
            Assert.That(rails[0].Below, Is.EqualTo(new[] { "engine" }));
        });
    }

    /// <summary>
    /// Two cards that feed different cards have a rail each, from the left, so you can see which feeds which.
    /// </summary>
    [Test]
    public void TwoCardsFeedingDifferentCards_HaveARailEach()
    {
        var spec = Spec([["project", "guide"], ["adr", "learn", "work"]],
            new CardLink("guide", "work"), new CardLink("project", "adr"), new CardLink("project", "learn"));

        var rails = CardRails.Of(spec);

        Assert.That(rails, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rails[0].Above, Is.EqualTo(new[] { "project" }));
            Assert.That(rails[0].Below, Is.EqualTo(new[] { "adr", "learn" }));
            Assert.That(rails[1].Above, Is.EqualTo(new[] { "guide" }));
            Assert.That(rails[1].Below, Is.EqualTo(new[] { "work" }));
        });
    }

    /// <summary>
    /// Two cards that share only one of the cards they feed still have a rail each: on one rail, the picture would
    /// say the second feeds a card it doesn't.
    /// </summary>
    [Test]
    public void TwoCardsThatShareOnlySomeOfTheirCards_HaveARailEach()
    {
        var spec = Spec([["a", "b"], ["x", "y"]],
            new CardLink("a", "x"), new CardLink("a", "y"), new CardLink("b", "y"));

        var rails = CardRails.Of(spec);

        Assert.That(rails.Select(rail => string.Join(",", rail.Above) + ">" + string.Join(",", rail.Below)),
            Is.EqualTo(new[] { "a>x,y", "b>y" }));
    }

    /// <summary>
    /// A link written from the lower card to the upper stands on the rail of the gap it crosses, its upper card
    /// above, whatever way its arrow points.
    /// </summary>
    [TestCase(CardLinkKind.Down)]
    [TestCase(CardLinkKind.Up)]
    [TestCase(CardLinkKind.Both)]
    public void ALinkWrittenUpwards_StandsInTheGapItCrosses(CardLinkKind kind)
    {
        var spec = Spec([["top"], ["middle"], ["bottom"]], new CardLink("bottom", "middle", kind));

        var rail = CardRails.Of(spec).Single();

        Assert.Multiple(() =>
        {
            Assert.That(rail.Gap, Is.EqualTo(1));
            Assert.That(rail.Above, Is.EqualTo(new[] { "middle" }));
            Assert.That(rail.Below, Is.EqualTo(new[] { "bottom" }));
            Assert.That(rail.Links.Single().Kind, Is.EqualTo(kind), "the link is kept as it was written");
        });
    }

    /// <summary>The rails come gap by gap from the top, whatever order the links were written in.</summary>
    [Test]
    public void TheRails_ComeFromTheTopGapDown()
    {
        var spec = Spec([["a"], ["b"], ["c"]], new CardLink("b", "c"), new CardLink("a", "b"));

        Assert.That(CardRails.Of(spec).Select(rail => rail.Gap), Is.EqualTo(new[] { 0, 1 }));
    }

    /// <summary>A picture with no links has no rails.</summary>
    [Test]
    public void APictureWithNoLinks_HasNoRails() =>
        Assert.That(CardRails.Of(Spec([["top"], ["left", "right"]])), Is.Empty);
}
