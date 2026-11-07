using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// The sign-in and register pages look like the rest of Puls (FX64, Louis: "this does not look like Puls, Puls has a
/// flashy UI, this is bland, and the black text on the orange is puke"): the button is the site's action, its
/// gradient with the text that goes on it, and Puls lays its own glass card over the water. The stylesheet holds the
/// look, so this holds the rules; how it looks is checked in the browser.
/// </summary>
public class AccountPageStyleTests
{
    /// <summary>The CSS the build makes for Puls, its components' bundle and the account pages' included.</summary>
    private static string Css => SassStylesheet.Of("louis-agent.puls").Read();

    /// <summary>The declarations of the first rule for this selector, or a failure naming it.</summary>
    private static string RuleOf(string selector)
    {
        var match = Regex.Match(Css, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(match.Success, Is.True, $"no rule for {selector}");
        return match.Groups[1].Value;
    }

    /// <summary>The button is the site's action: its gradient, and the text colour meant for it, never the page's.</summary>
    [Test]
    public void TheAccountButton_IsTheSitesAction_WithItsOwnTextColour()
    {
        string rule = RuleOf(".account-button");

        Assert.That(rule, Does.Contain("background: var(--web-cta)"));
        Assert.That(rule, Does.Contain("color: var(--web-on-primary)"),
            "the text that goes on an action; the page's background colour made it black on orange");
    }

    /// <summary>Puls's card is glass over the water, with a ring in its colours.</summary>
    [Test]
    public void PulsAccountCard_IsGlass_WithARing()
    {
        Assert.That(RuleOf(".puls-account .account-card"), Does.Contain("backdrop-filter"));
        Assert.That(RuleOf(".puls-account .account-card::before"), Does.Contain("mask-composite: exclude"));
    }

    /// <summary>Puls's button is a pill, like the landing page's.</summary>
    [Test]
    public void PulsAccountButton_IsAPill() =>
        Assert.That(RuleOf(".puls-account .account-button"), Does.Contain("border-radius: 999px"));
}
