using LouisAgent.Evals.Markdown;

namespace LouisAgent.Evals.Checks.Tests.Markdown;

/// <summary>The bold rows that close a shopping list, which now may say how many items they left out.</summary>
public class ListsTotalRowsTests
{
    /// <summary>A shopping list that ends with this total row.</summary>
    private static string ListWithTotal(string total) =>
        "| Item | To buy | Estimate |\n|---|---|---|\n| rice | rice 1 kg | ± R40 |\n" + total + "\n";

    /// <summary>A total row that says "unpriced" is still one row, with its amount in the last cell.</summary>
    [TestCase("| **Total** | 5 of 7 items | **± R312 + 2 items unpriced** |", "**± R312 + 2 items unpriced**")]
    [TestCase("| **Total** | 6 of 7 items | **± R312 + 1 item unpriced** |", "**± R312 + 1 item unpriced**")]
    [TestCase("| **Totaal** | 5 van 7 | **± R312 + 2 items sonder prys** |", "**± R312 + 2 items sonder prys**")]
    public void ATotalRowThatSaysWhatItLeftOut_IsStillTheTotalRow(string row, string lastCell)
    {
        var rows = Lists.TotalRows(ListWithTotal(row));

        Assert.That(rows.Single().Last(), Is.EqualTo(lastCell));
    }
}
