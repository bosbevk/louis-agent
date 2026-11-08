using System.Globalization;
using System.Text.Json;
using LouisAgent.CodeReview.Pages.Site;

namespace LouisAgent.CodeReview.Tests.Pages.Site;

/// <summary>
/// The Measured page shows its dollars in rand too (F109.4), and the rand come from the demo's rates file, the one the
/// app falls back on: the page quotes the file's rate and its date, never a number of its own.
/// </summary>
public class ProofPageTests
{
    /// <summary>The dollar figures the page quotes, as the ledger and the eval runs recorded them.</summary>
    private static readonly decimal[] Dollars = [0.66m, 0.13m, 0.37m];

    /// <summary>The rates file's rand rate and its date.</summary>
    private static (decimal Rate, string AsOf) ZarRate()
    {
        string text = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/config/rates.json"));
        using var json =
            JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

        return (json.RootElement.GetProperty("rates").GetProperty("ZAR").GetDecimal(),
            json.RootElement.GetProperty("as_of").GetString()!);
    }

    /// <summary>A rand figure the way the page writes it: R, two decimals.</summary>
    private static string Rand(decimal dollars, decimal rate) =>
        "R" + Math.Round(dollars * rate, 2).ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Every dollar figure has its rand beside it, at the file's rate, and the rate's date is on the page.</summary>
    [Test]
    public void TheRandFigures_ComeFromTheRatesFile()
    {
        using var ctx = new BunitContext();
        var (rate, asOf) = ZarRate();

        var cut = ctx.Render<ProofPage>();

        Assert.Multiple(() =>
        {
            Assert.That(Dollars.Select(dollars => Rand(dollars, rate)).Where(rand => !cut.Markup.Contains(rand)),
                Is.Empty);
            Assert.That(cut.Markup, Does.Contain(asOf));
            Assert.That(cut.Markup, Does.Contain(rate.ToString(CultureInfo.InvariantCulture)));
        });
    }

    /// <summary>The page says how a rand figure comes about: dollars from a dated price table, rand for display only.</summary>
    [TestCase("dated price table")]
    [TestCase("ledger")]
    [TestCase("for display")]
    public void ThePage_SaysHowTheRandWork(string words)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ProofPage>();

        Assert.That(cut.Markup, Does.Contain(words));
    }
}
