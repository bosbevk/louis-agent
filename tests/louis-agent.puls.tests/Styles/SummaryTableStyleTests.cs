using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// The by-category table reads as a table: its cells are spaced apart, its numbers line up on the right, and its mode
/// headings are headings. The first real one had none of that, so its columns ran into each other ("24 6 of 24
/// passed", "shop:no-branch3"); F18 step 5c, Louis: "look at how nice everything looks and then we see this". The
/// stylesheet spaces it, so this holds the rules; how it looks is checked in the browser.
/// </summary>
public class SummaryTableStyleTests
{
    /// <summary>The Puls stylesheet, every file it imports.</summary>
    private static string Css => PulsStylesheet.Read();

    /// <summary>The declarations of the first rule for this selector, or a failure naming it.</summary>
    private static string RuleOf(string selector)
    {
        var match = Regex.Match(Css, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(match.Success, Is.True, $"no rule for {selector}");
        return match.Groups[1].Value;
    }

    /// <summary>The cells have room around them, and the rows sit apart as rounded cards across the card.</summary>
    [Test]
    public void TheCells_AreSpacedApart_AndTheTableFillsItsCard()
    {
        Assert.That(RuleOf(".summary-table th, .summary-table td"), Does.Match(@"padding:\s*\d+px\s+\d+px"));
        Assert.That(RuleOf(".summary-table"),
            Does.Contain("width: 100%").And.Contain("border-collapse: separate").And
                .Match(@"border-spacing:\s*0\s+\d+px"), "rows apart, as rounded cards (6a.4)");
    }

    /// <summary>The numbers sit on the right, their digits under each other.</summary>
    [Test]
    public void TheNumbers_AreRightAlignedAndLineUp()
    {
        string rule = RuleOf(".summary-table .num");

        Assert.That(rule, Does.Contain("text-align: right").And.Contain("tabular-nums"));
    }

    /// <summary>
    /// The overall line is bold, and a mode's heading is an orange heading, not the size of the page title.
    /// </summary>
    [Test]
    public void TheOverallLine_StandsOut_AndTheModeHeadingIsHeadingSized()
    {
        Assert.That(RuleOf(".summary-table tr.overall td"),
            Does.Contain("font-weight: 600").And.Not.Contain("245, 130, 32"),
            "bold, not edged (Louis: an orange heading instead)");
        Assert.That(RuleOf(".summary-mode h3"), Does.Contain("var(--orange-text)"),
            "the mode's heading is the orange, as the run table's mode lines are");
        Assert.That(RuleOf(".summary-table .summary-row td"),
            Does.Contain("background: var(--bg)").And.Contain("border-top"), "each row its own rounded card");
        Assert.That(RuleOf(".summary-mode h3"), Does.Match(@"font-size:\s*(1\d|2[0-2])px"),
            "a heading, not the page's title");
    }

    /// <summary>The passed column is wide enough for "too few for a rate".</summary>
    [Test]
    public void ThePassedColumn_HasRoomForTooFewForARate() =>
        Assert.That(RuleOf(".summary-table .s-passed"), Does.Match(@"min-width:\s*\d+(rem|px)"));
}
