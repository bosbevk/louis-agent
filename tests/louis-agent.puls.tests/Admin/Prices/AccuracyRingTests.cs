using LouisAgent.Puls.Admin.Prices;

namespace LouisAgent.Puls.Tests.Admin.Prices;

// FX24 (Louis, 2026-10-26, on the accuracy donut: "all ours was guessed"): the cook gives estimates, so a mode whose 52 rows are all estimates drew 0 / 52, all "no price".
// An estimate is its own slice, counted as priced (as the row's chip counts it), apart from a guess (a price with no source) and from a row with no price.
// Written first: the ring knew right, guessed and no price.
public class AccuracyRingTests
{
    [Test]
    public void AModeWhoseRowsAreAllEstimates_IsAllEstimated_AndFullyCounted()
    {
        var ring = AccuracyRing.Of(priced: 0, noShop: 0, guessed: 0, estimated: 52, items: 52);

        Assert.That((ring.Right, ring.Estimated, ring.Guessed, ring.NoPrice), Is.EqualTo((0, 52, 0, 0)));
        Assert.That((ring.Share, ring.Over), Is.EqualTo(("100%", "52 / 52")));
    }

    [Test]
    public void ARowRightlyWithoutAPriceInATownWithNoShop_IsRight_AndAnEstimateBesideItIsEstimated()
    {
        var ring = AccuracyRing.Of(priced: 1, noShop: 2, guessed: 1, estimated: 3, items: 10);

        Assert.That((ring.Right, ring.Estimated, ring.Guessed, ring.NoPrice), Is.EqualTo((3, 3, 1, 3)));
        Assert.That(ring.Share, Is.EqualTo("60%"));
    }

    [Test]
    public void ARunFromBeforeTheEstimates_DrawsAsItDid()
    {
        var ring = AccuracyRing.Of(priced: 2, noShop: 0, guessed: 1, estimated: null, items: 5);

        Assert.That((ring.Right, ring.Estimated, ring.Guessed, ring.NoPrice, ring.Share), Is.EqualTo((2, 0, 1, 2, "40%")));
    }
}
