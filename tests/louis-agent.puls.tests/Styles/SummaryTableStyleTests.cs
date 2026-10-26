using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles;

// F18 step 5c (Louis, 2026-10-26, on the first real by-category table: "look at how nice everything looks and then we see this"): the table had no cell padding, so its columns ran into each
// other ("24 6 of 24 passed", "shop:no-branch3"), its numbers sat on the left, and its mode headings were page-sized. The stylesheet is what spaces it, so this holds the rules in the file;
// how it looks is checked in the browser.
public class SummaryTableStyleTests
{
    private static string Css => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src", "louis-agent.puls", "wwwroot", "css", "app.css"));

    private static string RuleOf(string selector)
    {
        var match = Regex.Match(Css, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(match.Success, Is.True, $"no rule for {selector}");
        return match.Groups[1].Value;
    }

    [Test]
    public void TheCells_AreSpacedApart_AndTheTableFillsItsCard()
    {
        Assert.That(RuleOf(".summary-table th, .summary-table td"), Does.Match(@"padding:\s*\d+px\s+\d+px"));
        Assert.That(RuleOf(".summary-table"), Does.Contain("width: 100%").And.Contain("border-collapse: separate").And.Match(@"border-spacing:\s*0\s+\d+px"), "rows apart, as rounded cards (6a.4)");
    }

    [Test]
    public void TheNumbers_AreRightAlignedAndLineUp()
    {
        string rule = RuleOf(".summary-table .num");

        Assert.That(rule, Does.Contain("text-align: right").And.Contain("tabular-nums"));
    }

    [Test]
    public void TheOverallLine_StandsOut_AndTheModeHeadingIsHeadingSized()
    {
        Assert.That(RuleOf(".summary-table tr.overall td"), Does.Contain("font-weight: 600").And.Contain("245, 130, 32"), "and edged orange, like the overall category card");
        Assert.That(RuleOf(".summary-table .summary-row td"), Does.Contain("background: var(--bg)").And.Contain("border-top"), "each row its own rounded card");
        Assert.That(RuleOf(".summary-mode h3"), Does.Match(@"font-size:\s*(1\d|2[0-2])px"), "a heading, not the page's title");
    }

    [Test]
    public void ThePassedColumn_HasRoomForTooFewForARate() =>
        Assert.That(RuleOf(".summary-table .s-passed"), Does.Match(@"min-width:\s*\d+(rem|px)"));
}
