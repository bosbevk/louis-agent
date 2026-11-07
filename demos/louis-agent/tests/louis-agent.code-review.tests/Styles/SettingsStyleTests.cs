using System.Text.RegularExpressions;

namespace LouisAgent.CodeReview.Tests.Styles;

/// <summary>
/// The sidebar's settings line up on every site (FX65, Louis on the coding agent's app: "margins not right"): the
/// animations checkbox had its rule only in Puls's stylesheet, so on any other site it sat flush against the edge, out
/// of line with the boxes above it, and the theme picker's list painted the page's near-white text on the browser's
/// white, so "Retro terminal" could not be read. The framework's toggle carries its own rule now, and a list's options
/// sit on the menu's colour.
/// </summary>
public class SettingsStyleTests
{
    /// <summary>The CSS the build makes for this site, the framework's components' bundle included.</summary>
    private static string Css => SassStylesheet.Of("louis-agent.code-review").Read();

    /// <summary>The declarations of the first rule for this selector, or a failure naming it.</summary>
    private static string RuleOf(string selector)
    {
        var match = Regex.Match(Css, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(match.Success, Is.True, $"no rule for {selector}");
        return match.Groups[1].Value;
    }

    /// <summary>The checkbox line keeps the settings' side margins, so it lines up with the boxes above it.</summary>
    [Test]
    public void TheMotionCheckbox_LinesUpWithTheOtherSettings()
    {
        string margin = Regex.Match(RuleOf(".settings"), @"margin:\s*([^;]+);").Groups[1].Value;
        string check = RuleOf(".settings-check");

        Assert.That(check, Does.Contain("display: flex"), "the box beside its words, not above them");
        Assert.That(check, Does.Contain($"margin: {margin}"), "the same side margins as the settings above it");
    }

    /// <summary>A picker's arrow sits a little in from its edge (Louis: "icon must move a bit in").</summary>
    [Test]
    public void APickersArrow_SitsInFromTheEdge() =>
        Assert.That(Css, Does.Match(@"\.settings select\s*\{\s*padding-right:\s*1[2-9]px"));

    /// <summary>A picker's open list is the menu's dark with the bright text, never bright text on the browser's white.</summary>
    [Test]
    public void APickersOptions_SitOnTheMenusColour() =>
        Assert.That(RuleOf(".settings select option"), Does.Contain("background: var(--web-menu-bg)"));
}
