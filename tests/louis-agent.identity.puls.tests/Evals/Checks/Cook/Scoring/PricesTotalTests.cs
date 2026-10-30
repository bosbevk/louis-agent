using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Scoring;

/// <summary>
/// The basket's total from an answer: the line that says it, else the sum of the priced rows, and which one it used.
/// When it's unsure, there's no total rather than a guess.
/// </summary>
public class PricesTotalTests
{
    /// <summary>Where a Checkers product link starts.</summary>
    private const string Checkers = "https://www.checkers.co.za/p/";

    /// <summary>A list row with its price linked to the shop.</summary>
    private static string Row(string item, string price, string site = Checkers) =>
        $"| {item} | {price} [kyk]({site}{item}) |";

    /// <summary>A shopping list with these rows under a Checkers column.</summary>
    private static string Table(params string[] rows) =>
        "| Bestanddeel | Checkers |\n|---|---|\n" + string.Join("\n", rows) + "\n";

    /// <summary>The total line is the total, however the amount is written.</summary>
    [TestCase("| **Totaal** | **R149,95** |", 149.95)]
    [TestCase("Totaal: R149,95", 149.95)]
    [TestCase("Total: R149.95", 149.95)]
    [TestCase("Totaal: R1 249,95", 1249.95)]
    [TestCase("Total: R1,249.95", 1249.95)]
    [TestCase("Totaal: R1.249,95", 1249.95)]
    [TestCase("Totaal ongeveer R150", 150)]
    [TestCase("**Totaal: R149,95**", 149.95)]
    public void TheTotalLineIsTheTotal_WhateverWayTheAmountIsWritten(string line, double expected)
    {
        var total = Prices.Total("Hier is die lys.\n" + line + "\nGeniet dit!");

        Assert.That(total, Is.EqualTo(new BasketTotal((decimal)expected, TotalSource.TotalLine)));
    }

    /// <summary>With no total line, the priced rows are added up.</summary>
    [Test]
    public void WithNoTotalLine_ItIsTheSumOfThePricedRows()
    {
        string answer = Table(Row("uie", "R45,99"), Row("kerrie", "R30,00"), Row("rys", "R12,50"));

        Assert.That(Prices.Total(answer), Is.EqualTo(new BasketTotal(88.49m, TotalSource.RowSum)));
    }

    /// <summary>A price with no link is a guess, and is left out of the sum.</summary>
    [Test]
    public void ARowWithAPriceButNoLink_IsAGuess_AndIsLeftOutOfTheSum()
    {
        string answer = Table(Row("uie", "R45,99"), "| sout | R5,00 |");

        Assert.That(Prices.Total(answer), Is.EqualTo(new BasketTotal(45.99m, TotalSource.RowSum)));
    }

    /// <summary>The list's own total row wins over adding up its rows.</summary>
    [Test]
    public void TheTotalLine_WinsOverTheRows()
    {
        string answer = Table(Row("uie", "R45,99"), "| **Totaal** | **R200,00** |");

        Assert.That(Prices.Total(answer), Is.EqualTo(new BasketTotal(200m, TotalSource.ListTotalRow)),
            "a bold total row of the list is read as the list's total row (FX24)");
    }

    /// <summary>Two total lines that disagree give no total.</summary>
    [Test]
    public void TotalLinesThatDisagree_AreNoTotal()
    {
        // A total for each shop: which is the basket's? It does not guess.
        Assert.That(Prices.Total("Checkers totaal: R150,00\nPick n Pay totaal: R162,00"), Is.Null);
    }

    /// <summary>One total line with two amounts gives no total either.</summary>
    [Test]
    public void OneTotalLineWithTwoAmounts_IsNoTotal() =>
        Assert.That(Prices.Total("| **Totaal** | **R150,00** | **R162,00** |"), Is.Null);

    /// <summary>A row priced at two shops can't be summed: which shop is the basket's isn't known.</summary>
    [Test]
    public void ARowPricedAtTwoShops_IsNoSum_BecauseWhichShopIsTheBasketsIsUnknown()
    {
        string answer = "| Bestanddeel | Checkers | Pick n Pay |\n|---|---|---|\n"
                        + "| uie | R45,99 [a](https://www.checkers.co.za/p/1) | R47,00 [b](https://www.pnp.co.za/p/1) |\n";

        Assert.That(Prices.Total(answer), Is.Null);
    }

    /// <summary>A pack's weight, or a multiplier, after the price is not part of the amount.</summary>
    [TestCase("R45 250 g", 45)]
    [TestCase("R12 500 ml", 12)]
    [TestCase("R45 1 kg", 45)]
    [TestCase("R45,99 250 g", 45.99)]
    [TestCase("R1 250,00 500 g", 1250)]
    // A multiplier after the price is not a unit: the price stays whole (a review found the unit check had read
    // R69,99 x 2 as R69, and lost R45 x 2).
    [TestCase("R69,99 x 2", 69.99)]
    [TestCase("R45 x 2", 45)]
    [TestCase("R19,99 × 2", 19.99)]
    [TestCase("R12 x 3", 12)]
    public void APacksWeightAfterThePrice_IsNotPartOfIt(string price, double expected)
    {
        // `R45 250 g` is R45 for a 250 g pack, not R45 250: a space before a group of three digits looks like a
        // thousands separator. A review caught it: it would have failed a good basket on the budget check.
        string answer = Table($"| mielies | {price} [kyk](https://www.checkers.co.za/p/mielies) |");

        Assert.That(Prices.Total(answer), Is.EqualTo(new BasketTotal((decimal)expected, TotalSource.RowSum)));
    }

    /// <summary>A thousands separator in a row still reads the whole amount.</summary>
    [Test]
    public void ARow_WithAThousandsSeparator_StillReadsItsWholeAmount() =>
        Assert.That(Prices.Total(Table("| braai | R1 250 [kyk](https://www.checkers.co.za/p/braai) |")),
            Is.EqualTo(new BasketTotal(1250m, TotalSource.RowSum)));

    /// <summary>A row priced as a range can't be summed.</summary>
    [Test]
    public void ARowWithARange_IsNoSum() =>
        Assert.That(Prices.Total(Table("| bees | R87,99–R103,99 [kyk](https://www.checkers.co.za/p/1) |")), Is.Null);

    /// <summary>An answer with no amount has no total.</summary>
    [TestCase("")]
    [TestCase("Ek kan nie pryse kry nie.")]
    public void AnAnswerWithNoAmount_HasNoTotal(string answer) => Assert.That(Prices.Total(answer), Is.Null);

    /// <summary>No answer has no total.</summary>
    [Test]
    public void ANullAnswerHasNoTotal() => Assert.That(Prices.Total(null), Is.Null);
}
