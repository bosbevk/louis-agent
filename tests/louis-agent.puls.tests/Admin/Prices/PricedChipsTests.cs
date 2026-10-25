using LouisAgent.Puls.Admin.Prices;

namespace LouisAgent.Puls.Tests.Admin.Prices;

// FX24 (Louis, 2026-10-26: "if we give estimates it must now count as priced"): the cook gives estimates while the stores' answer is awaited, so a list of five estimated
// rows is "priced 5 / 5", green, with the estimates said apart; it was a red "priced 0 / 5". Written first: the chip counted only the shop's own prices.
public class PricedChipsTests
{
    [Test]
    public void EstimatedRows_CountAsPriced_AndAreSaidApart()
    {
        var chips = PricedChips.Of(priced: 0, estimated: 5, items: 5, ratio: 1.0m);

        Assert.That(chips, Is.EqualTo(new[] { new PricedChip("priced 5 / 5", "ok"), new PricedChip("estimated 5", "") }));
    }

    [Test]
    public void AShopsOwnPrices_AndEstimates_AddUp()
    {
        var chips = PricedChips.Of(priced: 2, estimated: 2, items: 5, ratio: 0.8m);

        Assert.That(chips.Select(c => c.Text), Is.EqualTo(new[] { "priced 4 / 5", "estimated 2" }));
        Assert.That(chips[0].Kind, Is.EqualTo("ok"));
    }

    [Test]
    public void AListWithNoEstimates_IsTheChipItWas()
    {
        Assert.That(PricedChips.Of(priced: 1, estimated: 0, items: 4, ratio: 0.25m), Is.EqualTo(new[] { new PricedChip("priced 1 / 4", "bad") }));
        Assert.That(PricedChips.Of(priced: 3, estimated: 0, items: 5, ratio: 0.6m), Is.EqualTo(new[] { new PricedChip("priced 3 / 5", "") }));
    }

    [Test]
    public void AScoresDocumentFromBeforeEstimates_HasNone()
    {
        Assert.That(PricedChips.Of(priced: 2, estimated: null, items: 2, ratio: 1.0m).Select(c => c.Text), Is.EqualTo(new[] { "priced 2 / 2" }));
    }
}
