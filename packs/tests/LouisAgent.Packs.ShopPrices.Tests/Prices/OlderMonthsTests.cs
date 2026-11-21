using LouisAgent.Packs.ShopPrices.Estimates;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.Regions;
using LouisAgent.Packs.ShopPrices.Prices.Store;
using LouisAgent.Packs.ShopPrices.Tests.Prices.Store;
using LouisAgent.Packs.ShopPrices.Tools;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices;

/// <summary>
/// A product Stats SA left out of its newest month is priced from the newest month that has it, and the line says
/// which month (Louis's pick E1, 2026-11-04); a product the newest month has is priced from it.
/// </summary>
public class OlderMonthsTests
{
    private const string Source = "Stats SA, CPI average prices (P0141), all urban areas and provinces";

    private static PriceProduct Product(string name, decimal price) => new(name, [name.ToLowerInvariant()],
        [new PricePack("1 Kilogram", 1000, PackUnit.Gram, new Dictionary<string, decimal> { ["national"] = price })]);

    private static readonly PriceFile July = new(Source, new DateOnly(2026, 7, 1),
        [Product("White Rice", 26m), Product("Onions", 20m)]);

    private static readonly PriceFile August = new(Source, new DateOnly(2026, 8, 1), [Product("White Rice", 28m)]);

    private static BasketItem Line(string text) => BasketItem.Parse(text);

    /// <summary>The newest month prices what it has; an older one prices only what the newest lacks.</summary>
    [Test]
    public void AProductTheNewestMonthLacks_IsPricedFromAnOlderOne()
    {
        var book = new PriceBook([August], [July]);

        var rice = PriceEstimator.Estimate(Line("white rice 1 kg"), book, Region.National);
        var onions = PriceEstimator.Estimate(Line("onions 1 kg"), book, Region.National);

        Assert.That((rice.Rand, rice.Match!.File.AsOf), Is.EqualTo((28m, August.AsOf)));
        Assert.That((onions.Rand, onions.Match!.File.AsOf), Is.EqualTo((20m, July.AsOf)));
    }

    /// <summary>The line priced from an older month names that month, and isn't called our own row.</summary>
    [Test]
    public void TheLine_NamesTheOlderMonth()
    {
        var book = new PriceBook([August], [July]);
        var estimate = BasketEstimate.Of(["onions 1 kg"], book, Region.National, serves: null);

        string text = EstimatePrinter.Print(estimate, book, provinceUnknown: null);

        Assert.That(text, Does.Contain("Onions, 1 Kilogram at R20.00 (from July 2026, the newest month that prices it)"));
        Assert.That(text, Does.Not.Contain("our own estimate"));
    }

    /// <summary>The cache hands the book every older month of each source, so the tool can fall back.</summary>
    [Test]
    public async Task TheCache_ReadsTheOlderMonthsToo()
    {
        var cache = new PriceBookCache(new MemoryPriceStore(true, July, August), TimeProvider.System);

        var book = await cache.GetAsync(CancellationToken.None);

        Assert.That(book.Files.Select(f => f.AsOf), Is.EqualTo(new[] { August.AsOf }));
        Assert.That(book.Older.Select(f => f.AsOf), Is.EqualTo(new[] { July.AsOf }));
    }
}
