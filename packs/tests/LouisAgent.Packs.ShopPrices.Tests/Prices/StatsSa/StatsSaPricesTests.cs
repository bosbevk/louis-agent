using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices.StatsSa;

/// <summary>
/// Stats SA's rows turned into a price table a month, named from the names file: the proof is that the August 2026
/// release, read whole, gives back the table the pack ships for August.
/// </summary>
public class StatsSaPricesTests
{
    private static string Fixture(string name) => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name);

    private static StatsSaRows Read(string allUrban, string provinces)
    {
        using var urban = File.OpenRead(Fixture(allUrban));
        using var province = File.OpenRead(Fixture(provinces));
        return StatsSaSheets.Read(urban, province);
    }

    /// <summary>A product, its names and each pack with its prices, ordered so two tables compare by value.</summary>
    private static IEnumerable<string> Lines(PriceFile file) => file.Products.SelectMany(product =>
        product.Packs.Select(pack =>
            $"{product.Product} [{string.Join(", ", product.Names)}] {pack.Size} = {pack.Amount} {pack.Unit}: " +
            string.Join(", ", pack.Prices.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key} {p.Value:0.00}"))));

    /// <summary>The August release's August month is the shipped August table, product for product.</summary>
    [Test]
    public void TheAugustRelease_GivesTheShippedAugustTable()
    {
        var rows = Read("statssa-all-urban-202608.xlsx", "statssa-provinces-202608.xlsx");
        var shipped = PriceBook.Default.Files[0];

        var august = StatsSaPrices.ToFiles(rows.Rows, StatsSaNames.Default).Single(f => f.AsOf == new DateOnly(2026, 8, 1));

        Assert.That(august.Source, Is.EqualTo(shipped.Source));
        Assert.That(august.Products, Has.Count.EqualTo(220));
        Assert.That(Lines(august), Is.EqualTo(Lines(shipped)));
    }

    /// <summary>One table a month, oldest first, each holding only the packs priced that month.</summary>
    [Test]
    public void EachMonth_IsATableOfItsOwn()
    {
        var files = StatsSaPrices.ToFiles(Read("statssa-all-urban.xlsx", "statssa-provinces.xlsx").Rows,
            StatsSaNames.Default);

        Assert.That(files.Select(f => f.AsOf), Is.EqualTo(new[]
        {
            new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 1),
        }));
        var june = files[0];
        Assert.That(june.Products.Select(p => p.Product), Is.EqualTo(new[] { "White Rice" }));
        Assert.That(june.Products[0].Packs.Select(p => p.Size), Is.EqualTo(new[] { "1 Kilogram", "2 Kilogram" }));
        Assert.That(june.Products[0].Packs[0].Prices, Is.EquivalentTo(new Dictionary<string, decimal>
        {
            ["national"] = 27.10m,
            ["western-cape"] = 26.80m,
        }));
    }

    /// <summary>
    /// A product the names file lists takes its name and names from there; one it doesn't is named by its own tidied
    /// name, so a new product is still found, and the import lists it for a person to name.
    /// </summary>
    [Test]
    public void AProductTheNamesFileLacks_IsNamedByItsOwnName()
    {
        var names = new StatsSaNames([new StatsSaName("01111201", "White Rice", ["white rice", "rice"])]);

        var rows = Read("statssa-all-urban.xlsx", "statssa-provinces.xlsx").Rows;

        var july = StatsSaPrices.ToFiles(rows, names)[1];

        Assert.That(july.Products.Select(p => (p.Product, string.Join("|", p.Names))), Is.EqualTo(new[]
        {
            ("White Rice", "white rice|rice"), ("Onions - fresh", "onions - fresh"),
        }));
        Assert.That(names.Unnamed(rows), Is.EqualTo(new[] { "Onions - fresh" }));
    }
}
