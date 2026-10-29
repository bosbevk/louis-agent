using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// The admin page scrolls as one, and no section of it is squeezed to make room (Louis, 2026-10-29, a screenshot
/// while a run streamed: the "Runs" card under a tall live panel shrank to a sliver with a scrollbar of its own). The
/// page is a column of flex items in a box that scrolls; an item that scrolls itself may shrink to nothing there, so
/// the items keep their height and the page scrolls instead. The stylesheet holds the rule; how it looks is checked
/// in the browser.
/// </summary>
public class AdminPageStyleTests
{
    /// <summary>The Puls stylesheet.</summary>
    private static string Css =>
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src", "louis-agent.puls", "wwwroot", "css", "app.css"));

    /// <summary>The declarations of the rule whose selector is exactly this, or a failure naming it.</summary>
    private static string RuleOf(string selector)
    {
        var match = Regex.Match(Css, @"(?m)^" + Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(match.Success, Is.True, $"no rule for {selector}");
        return match.Groups[1].Value;
    }

    /// <summary>
    /// The page's sections keep their height, so the runs list under a tall live run is never squeezed.
    /// </summary>
    [Test]
    public void ThePagesSections_NeverShrink_SoThePageScrollsInstead()
    {
        Assert.That(RuleOf(".admin-main"), Does.Contain("overflow: auto").And.Contain("flex-direction: column"),
            "the page is a scrolling column");
        Assert.That(RuleOf(".admin-main > *"), Does.Match(@"flex-shrink:\s*0"),
            "a section that scrolls itself (the runs list) would shrink to a sliver otherwise");
    }
}
