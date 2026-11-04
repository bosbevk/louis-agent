using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices.StatsSa;

/// <summary>
/// Stats SA's pack sizes as numbers we can scale by: every way of writing one found in the releases from January 2017
/// to August 2026, each read as the shipped August file reads it.
/// </summary>
public class PackSizeTests
{
    /// <summary>A weight or a volume, with a comma or a point, in grams or millilitres.</summary>
    [TestCase("1 Kilogram", 1000, PackUnit.Gram)]
    [TestCase("1,5 Kilogram", 1500, PackUnit.Gram)]
    [TestCase("1.75 Kilogram", 1750, PackUnit.Gram)]
    [TestCase("62,5 Gram", 62.5, PackUnit.Gram)]
    [TestCase("1 Litre", 1000, PackUnit.Millilitre)]
    [TestCase("1.8 Litre", 1800, PackUnit.Millilitre)]
    [TestCase("330 Millilitre", 330, PackUnit.Millilitre)]
    [TestCase("10 X 18 Gram", 180, PackUnit.Gram)]
    [TestCase("6 X 275 Millilitre", 1650, PackUnit.Millilitre)]
    [TestCase("Per Kilogram", 1000, PackUnit.Gram)]
    [TestCase("Per kilogram", 1000, PackUnit.Gram)]
    [TestCase("Per Litre", 1000, PackUnit.Millilitre)]
    public void AWeightOrAVolume_IsReadInGramsOrMillilitres(string size, decimal amount, PackUnit unit) =>
        Assert.That(PackSize.Read(size), Is.EqualTo(new PackSize(amount, unit)));

    /// <summary>A count of items, however Stats SA words it.</summary>
    [TestCase("1 Unit", 1)]
    [TestCase("Each", 1)]
    [TestCase("Per Bunch", 1)]
    [TestCase("Per Pack", 1)]
    [TestCase("Box Of 12", 12)]
    [TestCase("For 2500", 2500)]
    [TestCase("Pack Of 30", 30)]
    [TestCase("Set Of 5", 5)]
    [TestCase("Half cabbage", 0.5)]
    public void ACount_IsReadAsItems(string size, decimal amount) =>
        Assert.That(PackSize.Read(size), Is.EqualTo(new PackSize(amount, PackUnit.Each)));

    /// <summary>A size we can't scale by is Other with no amount, never a guess.</summary>
    [TestCase("32 Inch")]
    [TestCase("Pack Of 2 Ply 18 Rolls")]
    [TestCase("Pack Of 3 Pairs")]
    [TestCase("Pallet of 1000 Bricks")]
    [TestCase("Per Hour")]
    [TestCase("Per Square Metre")]
    [TestCase("")]
    public void ASizeWeCantScaleBy_IsOther(string size) =>
        Assert.That(PackSize.Read(size), Is.EqualTo(new PackSize(null, PackUnit.Other)));
}
