using LouisAgent.Packs.ShopPrices.Estimates;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Regions;
using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Tests.Estimates;

/// <summary>
/// A line is priced from a small table we control: the same pack when it is there, else scaled from the nearest;
/// the province first, the country when the province has no price; null when nothing fits, never R0.
/// </summary>
public class PriceEstimatorTests
{
    /// <summary>Two tables like the shipped ones: "Stats SA" first, "ours" after.</summary>
    private static readonly PriceBook Table = new(
    [
        new PriceFile("Stats SA", new DateOnly(2026, 8, 1),
        [
            new PriceProduct("Beef mince", ["beef mince", "mince"],
            [
                Pack("Per Kilogram", 1000, PackUnit.Gram, ("national", 124.92m), ("western-cape", 130.00m)),
                Pack("1 Kilogram", 1000, PackUnit.Gram, ("national", 155.37m)),
            ]),
            new PriceProduct("Eggs", ["eggs"],
            [
                Pack("For 6", 6, PackUnit.Each, ("national", 22.97m)),
                Pack("For 18", 18, PackUnit.Each, ("national", 60.29m)),
            ]),
            new PriceProduct("Cabbage", ["cabbage"], [Pack("Each", 1, PackUnit.Each, ("national", 22.96m))]),
            new PriceProduct("Unpriced", ["unpriced"], [Pack("1 Kilogram", 1000, PackUnit.Gram)]),
        ]),
        new PriceFile("ours", new DateOnly(2026, 11, 1),
            [new PriceProduct("Coconut milk", ["coconut milk"], [Pack("400 Millilitre", 400, PackUnit.Millilitre, ("national", 29.99m))])]),
    ]);

    /// <summary>A pack with its prices by region key.</summary>
    private static PricePack Pack(string size, decimal amount, PackUnit unit, params (string Key, decimal Price)[] prices) =>
        new(size, amount, unit, prices.ToDictionary(p => p.Key, p => p.Price));

    /// <summary>The same size the table prices is that pack's price, times the packs.</summary>
    [TestCase("eggs 18", 60)]
    [TestCase("eggs 6 x 2", 46)]
    [TestCase("coconut milk 400 ml", 30)]
    public void TheSameSize_IsThatPacksPrice(string line, decimal rand)
    {
        var estimate = PriceEstimator.Estimate(BasketItem.Parse(line), Table, Region.National);

        Assert.That(estimate.Kind, Is.EqualTo(EstimateKind.SamePack));
        Assert.That(estimate.Rand, Is.EqualTo(rand));
    }

    /// <summary>Another size is scaled from the nearest, the table's first on a tie (the price per kilogram).</summary>
    [TestCase("beef mince 500 g", 62)]
    [TestCase("beef mince 1.5 kg", 187)]
    [TestCase("eggs 12", 40)]
    public void AnotherSize_IsScaledFromTheNearest(string line, decimal rand)
    {
        var estimate = PriceEstimator.Estimate(BasketItem.Parse(line), Table, Region.National);

        Assert.That(estimate.Kind, Is.EqualTo(EstimateKind.Scaled));
        Assert.That(estimate.Rand, Is.EqualTo(rand));
    }

    /// <summary>The province's price wins; where it has none, the country's stands in, and says so.</summary>
    [Test]
    public void TheProvince_WinsOverTheCountry_WhereItHasAPrice()
    {
        var mince = PriceEstimator.Estimate(BasketItem.Parse("mince 1 kg"), Table, Region.WesternCape);
        var eggs = PriceEstimator.Estimate(BasketItem.Parse("eggs 18"), Table, Region.WesternCape);

        Assert.That(mince.Rand, Is.EqualTo(130));
        Assert.That(mince.Price!.Region, Is.EqualTo(Region.WesternCape));
        Assert.That(eggs.Price!.Region, Is.EqualTo(Region.National));
    }

    /// <summary>No size given is one of the table's packs, the first it prices.</summary>
    [Test]
    public void NoSize_IsTheTablesFirstPack()
    {
        var estimate = PriceEstimator.Estimate(BasketItem.Parse("cabbage"), Table, Region.National);

        Assert.That(estimate.Kind, Is.EqualTo(EstimateKind.TablePack));
        Assert.That(estimate.Rand, Is.EqualTo(23));
    }

    /// <summary>What we can't price is null, never R0: no product, no price, or a size in another unit.</summary>
    [TestCase("saffron 1 g", EstimateKind.NotFound)]
    [TestCase("unpriced 1 kg", EstimateKind.NotFound)]
    [TestCase("cabbage 2 kg", EstimateKind.OtherUnit)]
    public void WhatWeCantPrice_IsNull(string line, EstimateKind kind)
    {
        var estimate = PriceEstimator.Estimate(BasketItem.Parse(line), Table, Region.National);

        Assert.That(estimate.Kind, Is.EqualTo(kind));
        Assert.That(estimate.Rand, Is.Null);
    }

    /// <summary>The total adds the whole-rand estimates shown, leaves out the unknown, and shares per person.</summary>
    [Test]
    public void TheTotal_AddsTheShownEstimates_AndSharesPerPerson()
    {
        var basket = BasketEstimate.Of(["beef mince 500 g", "eggs 18", "saffron 1 g", " "], Table, Region.National, 4);

        Assert.That(basket.Lines, Has.Count.EqualTo(3));
        Assert.That(basket.Total, Is.EqualTo(62 + 60));
        Assert.That(basket.Unpriced.Select(l => l.Item.Text), Is.EqualTo(new[] { "saffron 1 g" }));
        Assert.That(basket.PerPerson, Is.EqualTo(31));
    }

    /// <summary>No number of people, no price per person.</summary>
    [Test]
    public void NoPeople_NoPricePerPerson() =>
        Assert.That(BasketEstimate.Of(["eggs 18"], Table, Region.National, null).PerPerson, Is.Null);
}
