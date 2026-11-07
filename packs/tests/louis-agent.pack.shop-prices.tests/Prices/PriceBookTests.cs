using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Regions;
using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices;

/// <summary>
/// The shipped tables read, every region key is one we know, and a shopping-list line finds the product it means.
/// </summary>
public class PriceBookTests
{
    /// <summary>Stats SA's August 2026 prices come first, our own rows after them.</summary>
    [Test]
    public void TheShippedTables_AreStatsSasMonth_ThenOurs()
    {
        var files = PriceBook.Default.Files;

        Assert.That(files, Has.Count.EqualTo(2));
        Assert.That(files[0].Source, Does.StartWith("Stats SA"));
        Assert.That(files[0].AsOf, Is.EqualTo(new DateOnly(2026, 8, 1)));
        Assert.That(files[0].Products, Has.Count.GreaterThan(200));
        Assert.That(files[1].Source, Does.Contain("own estimate"));
    }

    /// <summary>A key no region has would be a price nobody could ever be given.</summary>
    [Test]
    public void EveryPricesKey_IsARegion()
    {
        var keys = PriceBook.Default.Files.SelectMany(f => f.Products).SelectMany(p => p.Packs)
            .SelectMany(pack => pack.Prices.Keys).Distinct().ToList();

        Assert.That(keys, Is.SubsetOf(SettingChoice.Names<Region>()));
        Assert.That(keys, Has.Count.EqualTo(10), "the country and the nine provinces");
    }

    /// <summary>A pack we scale by has its amount; only a size we can't scale by goes without.</summary>
    [Test]
    public void EveryScalablePack_HasItsAmount()
    {
        var packs = PriceBook.Default.Files.SelectMany(f => f.Products).SelectMany(p => p.Packs).ToList();

        Assert.That(packs.Where(p => p.Unit != PackUnit.Other), Has.All.Matches<PricePack>(p => p.Amount > 0));
    }

    /// <summary>The line finds its product by the longest name whose words are all in it.</summary>
    [TestCase("beef mince", "Beef mince")]
    [TestCase("mince", "Beef mince")]
    [TestCase("chicken thighs", "Chicken Portions - Fresh")]
    [TestCase("brown onions", "Onions - fresh")]
    [TestCase("tomato sauce", "Tomato sauce")]
    [TestCase("tomatoes", "Tomatoes - fresh")]
    [TestCase("tinned tomatoes", "Tinned tomatoes")]
    [TestCase("coconut milk", "Coconut milk")]
    [TestCase("milk", "Full cream milk - fresh")]
    [TestCase("chicken stock cubes", "Stock cubes/powder")]
    [TestCase("maalvleis", "Beef mince")]
    [TestCase("mielie meal", "Maize meal")]
    public void ALine_FindsTheProductItMeans(string line, string product) =>
        Assert.That(PriceBook.Default.Find(ProductWords.Of(line))?.Product.Product, Is.EqualTo(product));

    /// <summary>A product no table prices is found nowhere, rather than guessed.</summary>
    [Test]
    public void AProductNoTablePrices_IsNotFound() =>
        Assert.That(PriceBook.Default.Find(ProductWords.Of("saffron")), Is.Null);
}
