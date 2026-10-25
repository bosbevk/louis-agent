
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Scoring;

// F18 step 5.4: the basket's total from an answer: the line that says it, else the sum of the priced rows, and which one it used. A
// reading it is unsure of is null, never a guess. Written first: the reader threw NotImplementedException on the red run.
public class PricesTotalTests
{
    private const string Checkers = "https://www.checkers.co.za/p/";

    private static string Row(string item, string price, string site = Checkers) => $"| {item} | {price} [kyk]({site}{item}) |";

    private static string Table(params string[] rows) => "| Bestanddeel | Checkers |\n|---|---|\n" + string.Join("\n", rows) + "\n";

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

    [Test]
    public void WithNoTotalLine_ItIsTheSumOfThePricedRows()
    {
        string answer = Table(Row("uie", "R45,99"), Row("kerrie", "R30,00"), Row("rys", "R12,50"));

        Assert.That(Prices.Total(answer), Is.EqualTo(new BasketTotal(88.49m, TotalSource.RowSum)));
    }

    [Test]
    public void ARowWithAPriceButNoLink_IsAGuess_AndIsLeftOutOfTheSum()
    {
        string answer = Table(Row("uie", "R45,99"), "| sout | R5,00 |");

        Assert.That(Prices.Total(answer), Is.EqualTo(new BasketTotal(45.99m, TotalSource.RowSum)));
    }

    [Test]
    public void TheTotalLine_WinsOverTheRows()
    {
        string answer = Table(Row("uie", "R45,99"), "| **Totaal** | **R200,00** |");

        Assert.That(Prices.Total(answer), Is.EqualTo(new BasketTotal(200m, TotalSource.TotalLine)));
    }

    [Test]
    public void TotalLinesThatDisagree_AreNoTotal()
    {
        // A total for each shop: which is the basket's? It does not guess.
        Assert.That(Prices.Total("Checkers totaal: R150,00\nPick n Pay totaal: R162,00"), Is.Null);
    }

    [Test]
    public void OneTotalLineWithTwoAmounts_IsNoTotal() =>
        Assert.That(Prices.Total("| **Totaal** | **R150,00** | **R162,00** |"), Is.Null);

    [Test]
    public void ARowPricedAtTwoShops_IsNoSum_BecauseWhichShopIsTheBasketsIsUnknown()
    {
        string answer = "| Bestanddeel | Checkers | Pick n Pay |\n|---|---|---|\n"
            + "| uie | R45,99 [a](https://www.checkers.co.za/p/1) | R47,00 [b](https://www.pnp.co.za/p/1) |\n";

        Assert.That(Prices.Total(answer), Is.Null);
    }

    [TestCase("R45 250 g", 45)]
    [TestCase("R12 500 ml", 12)]
    [TestCase("R45 1 kg", 45)]
    [TestCase("R45,99 250 g", 45.99)]
    [TestCase("R1 250,00 500 g", 1250)]
    public void APacksWeightAfterThePrice_IsNotPartOfIt(string price, double expected)
    {
        // `R45 250 g` is R45 for a 250 g pack, not R45 250: a space before a group of three digits looks like a thousands separator.
        // Reviewed on 5.4: it would have failed a good basket on the budget grader.
        string answer = Table($"| mielies | {price} [kyk](https://www.checkers.co.za/p/mielies) |");

        Assert.That(Prices.Total(answer), Is.EqualTo(new BasketTotal((decimal)expected, TotalSource.RowSum)));
    }

    [Test]
    public void ARow_WithAThousandsSeparator_StillReadsItsWholeAmount() =>
        Assert.That(Prices.Total(Table("| braai | R1 250 [kyk](https://www.checkers.co.za/p/braai) |")), Is.EqualTo(new BasketTotal(1250m, TotalSource.RowSum)));

    [Test]
    public void ARowWithARange_IsNoSum() =>
        Assert.That(Prices.Total(Table("| bees | R87,99–R103,99 [kyk](https://www.checkers.co.za/p/1) |")), Is.Null);

    [TestCase("")]
    [TestCase("Ek kan nie pryse kry nie.")]
    public void AnAnswerWithNoAmount_HasNoTotal(string answer) => Assert.That(Prices.Total(answer), Is.Null);

    [Test]
    public void ANullAnswerHasNoTotal() => Assert.That(Prices.Total(null), Is.Null);
}
