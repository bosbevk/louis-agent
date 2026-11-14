using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Cards.Rails;

namespace LouisAgent.Diagrams.Tests.Cards.Rails;

/// <summary>
/// Where a gap's rails stand (F134.9): each end under or over the middle of its card, two rails that are clear of
/// each other at one height, and one that would lie on another a step under it.
/// </summary>
public class RailPlacesTests
{
    /// <summary>A rail in the first gap, its links written down from each card above to each card below.</summary>
    private static Rail Rail(string[] above, string[] below, CardLinkKind kind = CardLinkKind.Down) =>
        new(0, above, below, [.. above.SelectMany(from => below.Select(to => new CardLink(from, to, kind)))]);

    /// <summary>One rail's ends stand at the middle of their cards, and it spans from the first to the last.</summary>
    [Test]
    public void ARailsEnds_StandAtTheirCardsMiddles()
    {
        var centres = new Dictionary<string, double> { ["top"] = 448, ["a"] = 142, ["b"] = 448, ["c"] = 754 };

        var span = RailPlaces.Across([Rail(["top"], ["a", "b", "c"])], centres).Single();

        Assert.Multiple(() =>
        {
            Assert.That(span.Spots.Select(spot => (spot.Card, spot.X, spot.Above)), Is.EqualTo(new[]
            {
                ("top", 448d, true), ("a", 142d, false), ("b", 448d, false), ("c", 754d, false)
            }));
            Assert.That((span.Left, span.Right, span.Level), Is.EqualTo((142d, 754d, 0)));
        });
    }

    /// <summary>Two rails of one gap that are clear of each other stand at one height, side by side.</summary>
    [Test]
    public void TwoRailsClearOfEachOther_ShareAHeight()
    {
        var centres = new Dictionary<string, double>
        {
            ["one"] = 213,
            ["two"] = 682,
            ["a"] = 142,
            ["b"] = 448,
            ["c"] = 754
        };

        var spans = RailPlaces.Across([Rail(["one"], ["a", "b"]), Rail(["two"], ["c"])], centres);

        Assert.That(spans.Select(span => span.Level), Is.EqualTo(new[] { 0, 0 }));
    }

    /// <summary>
    /// Two rails that would lie on one another stand a step apart, and the card both feed has an end for each, a
    /// step apart too, in the rails' order.
    /// </summary>
    [Test]
    public void TwoRailsThatOverlap_StandAStepApart()
    {
        var centres = new Dictionary<string, double> { ["one"] = 213, ["two"] = 682, ["a"] = 213, ["b"] = 682 };

        var spans = RailPlaces.Across([Rail(["one"], ["a", "b"]), Rail(["two"], ["b"])], centres);

        Assert.Multiple(() =>
        {
            Assert.That(spans.Select(span => span.Level), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(spans[0].Spots.Single(spot => spot.Card == "b").X, Is.EqualTo(682 - RailPlaces.Step / 2));
            Assert.That(spans[1].Spots.Single(spot => spot is { Card: "b", Above: false }).X,
                Is.EqualTo(682 + RailPlaces.Step / 2));
        });
    }

    /// <summary>
    /// A gap is as tall as its rails need: a stem, a drop, and a step for each height after the first. With no
    /// rail it needs nothing.
    /// </summary>
    [Test]
    public void AGapsRoom_IsWhatItsRailsNeed()
    {
        var centres = new Dictionary<string, double> { ["one"] = 213, ["two"] = 682, ["a"] = 213, ["b"] = 682 };
        var one = RailPlaces.Across([Rail(["one"], ["a"])], centres);
        var two = RailPlaces.Across([Rail(["one"], ["a", "b"]), Rail(["two"], ["b"])], centres);

        Assert.Multiple(() =>
        {
            Assert.That(RailPlaces.Room([]), Is.Zero);
            Assert.That(RailPlaces.Room(one), Is.EqualTo(RailPlaces.Stem + RailPlaces.Drop));
            Assert.That(RailPlaces.Room(two), Is.EqualTo(RailPlaces.Stem + RailPlaces.Step + RailPlaces.Drop));
        });
    }

    /// <summary>
    /// An arrowhead belongs on the end a link points at: the second card for a link down, the first for one up,
    /// and both for one both ways.
    /// </summary>
    [TestCase(CardLinkKind.Down, false, true)]
    [TestCase(CardLinkKind.Up, true, false)]
    [TestCase(CardLinkKind.Both, true, true)]
    public void AnArrowhead_BelongsOnTheEndALinkPointsAt(CardLinkKind kind, bool atFirst, bool atSecond)
    {
        var rail = Rail(["top"], ["a"], kind);

        Assert.That((RailPlaces.PointsAt(rail, "top"), RailPlaces.PointsAt(rail, "a")),
            Is.EqualTo((atFirst, atSecond)));
    }
}
