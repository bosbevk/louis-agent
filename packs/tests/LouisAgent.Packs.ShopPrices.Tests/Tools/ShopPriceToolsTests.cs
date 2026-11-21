using LouisAgent.Packs.ShopPrices.Prices.Store;
using LouisAgent.Packs.ShopPrices.Tools;

namespace LouisAgent.Packs.ShopPrices.Tests.Tools;

/// <summary>
/// The tool prices a list from the shipped tables and writes it for the model: the source, a row per line, the
/// total, the price per person, what it couldn't price and the credit line.
/// </summary>
public class ShopPriceToolsTests
{
    /// <summary>The tool over the shipped tables, as a host without a price store runs it.</summary>
    private static readonly ShopPriceTools Tools = new(new PriceBookCache(new ShippedPrices(), TimeProvider.System));

    /// <summary>A Western Cape list: Stats SA's provincial prices, scaled, totalled and shared, and credited.</summary>
    [Test]
    public async Task AList_IsPricedTotalledAndCredited()
    {
        string result = await Tools.EstimatePrices(
            ["beef mince 500 g", "eggs 18", "coconut milk 400 ml", "saffron 1 g"],
            "Western Cape", 4);

        Assert.That(result,
            Does.Contain("Estimated prices for the Western Cape, from Stats SA's average prices for August 2026"));
        Assert.That(result, Does.Contain("| beef mince 500 g | Beef mince, Per Kilogram at R"));
        Assert.That(result,
            Does.Contain(
                "| coconut milk 400 ml | Coconut milk, 400 Millilitre at R29.99 (national, no price for the " +
                "Western Cape) (our own estimate, not Stats SA's) | ± R30 |"));
        Assert.That(result, Does.Contain("| saffron 1 g | no price for this product | ? |"));
        Assert.That(result, Does.Contain("| **Total** | 3 of 4 items |"));
        Assert.That(result, Does.Contain("| **Per person** | for 4 |"));
        Assert.That(result, Does.Contain("No estimate for: saffron 1 g."));
        Assert.That(result,
            Does.EndWith(
                "\"Prices are estimates from Stats SA's average prices (the Western Cape, August 2026), " +
                "not checked at a shop.\""));
    }

    /// <summary>The total in the result is the sum of the rows above it, to the rand.</summary>
    [Test]
    public async Task TheTotal_IsTheSumOfTheRows()
    {
        string result =
            await Tools.EstimatePrices(["rice 1 kg", "onions 1 kg", "beef mince 500 g", "tinned tomatoes 410 g x 2"]);
        var rows = result.Split('\n').Where(l => l.StartsWith("| ") && l.Contains("± R")).ToList();

        decimal Rand(string row) => decimal.Parse(row[(row.LastIndexOf("± R", StringComparison.Ordinal) + 3)..]
            .TrimEnd(' ', '|', '*', '\r'));

        Assert.That(rows.Where(r => !r.StartsWith("| **")).Sum(Rand),
            Is.EqualTo(Rand(rows.Single(r => r.StartsWith("| **Total")))));
        Assert.That(result, Does.Contain("Estimated prices for South Africa"));
    }

    /// <summary>A list that is all priced prints its total as before, with nothing about what it left out.</summary>
    [Test]
    public async Task AListThatIsAllPriced_SaysNothingUnpriced()
    {
        string result = await Tools.EstimatePrices(["rice 1 kg", "onions 1 kg"], "Western Cape", 2);

        Assert.That(result, Does.Not.Contain("unpriced"));
        Assert.That(result, Does.Match(@"\| \*\*Total\*\* \| 2 of 2 items \| \*\*± R\d+\*\* \|"));
    }

    /// <summary>One line with no price: the total and the per-person price both say one item is unpriced.</summary>
    [Test]
    public async Task OneUnpricedLine_IsSaidInTheTotalAndThePerPerson()
    {
        string result = await Tools.EstimatePrices(["rice 1 kg", "saffron 1 g"], "Western Cape", 2);

        Assert.That(result, Does.Match(@"\| \*\*Total\*\* \| 1 of 2 items \| \*\*± R\d+ \+ 1 item unpriced\*\* \|"));
        Assert.That(result, Does.Match(@"\| \*\*Per person\*\* \| for 2 \| \*\*± R\d+ \+ 1 item unpriced\*\* \|"));
    }

    /// <summary>Several lines with no price: the total says how many items it left out.</summary>
    [Test]
    public async Task SeveralUnpricedLines_AreCountedInTheTotalAndThePerPerson()
    {
        string result = await Tools.EstimatePrices(["rice 1 kg", "saffron 1 g", "caviar 50 g"], "Western Cape", 2);

        Assert.That(result, Does.Match(@"\| \*\*Total\*\* \| 1 of 3 items \| \*\*± R\d+ \+ 2 items unpriced\*\* \|"));
        Assert.That(result, Does.Match(@"\| \*\*Per person\*\* \| for 2 \| \*\*± R\d+ \+ 2 items unpriced\*\* \|"));
    }

    /// <summary>An unknown province says so and uses the country's averages, rather than failing the list.</summary>
    [Test]
    public async Task AnUnknownProvince_IsSaid_AndTheCountryAnswers()
    {
        string result = await Tools.EstimatePrices(["eggs 18"], "Stellenbosch");

        Assert.That(result, Does.StartWith("No province 'Stellenbosch' is known, so the national averages are used."));
        Assert.That(result, Does.Contain("western-cape"));
        Assert.That(result, Does.Contain("Estimated prices for South Africa"));
    }

    /// <summary>An empty list asks for one; a list too long asks for a second call.</summary>
    [Test]
    public async Task AnEmptyOrTooLongList_SaysWhatToSend()
    {
        Assert.That(await Tools.EstimatePrices([" ", ""]), Does.StartWith("EstimatePrices needs the shopping list"));
        Assert.That(await Tools.EstimatePrices([.. Enumerable.Repeat("eggs 6", 61)]),
            Does.StartWith("At most 60 lines"));
    }
}
