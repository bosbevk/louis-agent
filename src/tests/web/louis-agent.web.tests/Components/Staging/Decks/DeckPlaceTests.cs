using LouisAgent.Web.Components.Staging.Decks;

namespace LouisAgent.Web.Tests.Components.Staging.Decks;

/// <summary>
/// F134.1: a deck shows one thing forward and the rest behind it. Where each one stands is a plain rule, kept out of
/// the component so it is tested with no browser: how far from the middle, counted the short way round, so the last
/// item stands just left of the first and not four places to its right.
/// </summary>
public class DeckPlaceTests
{
    /// <summary>How far every item of a deck stands from the one forward.</summary>
    private static int[] Places(int forward, int count) =>
        Enumerable.Range(0, count).Select(index => DeckPlace.Of(index, forward, count)).ToArray();

    /// <summary>With five items, two stand to each side of the one forward, whichever that is.</summary>
    [TestCase(0, new[] { 0, 1, 2, -2, -1 })]
    [TestCase(1, new[] { -1, 0, 1, 2, -2 })]
    [TestCase(4, new[] { 1, 2, -2, -1, 0 })]
    public void EachItem_StandsTheShortWayRound(int forward, int[] places) =>
        Assert.That(Places(forward, 5), Is.EqualTo(places));

    /// <summary>With an even count, the one opposite the middle stands to the right.</summary>
    [Test]
    public void WithAnEvenCount_TheOppositeItemStandsToTheRight() =>
        Assert.That(Places(0, 4), Is.EqualTo(new[] { 0, 1, 2, -1 }));

    /// <summary>One item alone stands in the middle, and a deck of none asks for nothing.</summary>
    [Test]
    public void OneItem_StandsInTheMiddle() =>
        Assert.Multiple(() =>
        {
            Assert.That(Places(0, 1), Is.EqualTo(new[] { 0 }));
            Assert.That(Places(0, 0), Is.Empty);
        });

    /// <summary>A step past the last item comes round to the first, and a step before the first to the last.</summary>
    [TestCase(4, 1, 0)]
    [TestCase(0, -1, 4)]
    [TestCase(2, 1, 3)]
    public void AStepPastTheEnd_ComesRound(int forward, int by, int moved) =>
        Assert.That(DeckPlace.Moved(forward, by, 5), Is.EqualTo(moved));

    /// <summary>A deck of none has nowhere to step to.</summary>
    [Test]
    public void ADeckOfNone_StaysWhereItIs() =>
        Assert.That(DeckPlace.Moved(0, 1, 0), Is.Zero);
}
