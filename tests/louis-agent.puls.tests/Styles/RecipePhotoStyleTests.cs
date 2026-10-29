using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// The recipe's photo is a fixed box with rounded corners, not the page's full-size picture: a 1024-wide photo filled
/// the chat (FX24, Louis: "make the image a fixed size to fit in our chat screen", "give it some rounded corners").
/// The product photos in a shopping list's cells keep their own, smaller size. The stylesheet sizes it, so this holds
/// the rule; how it looks is checked in the browser.
/// </summary>
public class RecipePhotoStyleTests
{
    /// <summary>The Puls stylesheet, every file it imports.</summary>
    private static string Css => Stylesheet.Of("louis-agent.puls").Read();

    /// <summary>The declarations of the first rule for this selector, or a failure naming it.</summary>
    private static string RuleOf(string selector)
    {
        var match = Regex.Match(Css, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(match.Success, Is.True, $"no rule for {selector}");
        return match.Groups[1].Value;
    }

    /// <summary>The photo fits the chat, takes the same room tall or wide, and is cropped, never stretched.</summary>
    [Test]
    public void TheRecipePhoto_IsAFixedBox_ThatFitsTheChat()
    {
        string rule = RuleOf(".markdown img:not(td img)");

        Assert.That(rule, Does.Contain("width: 100%"), "never wider than the chat");
        Assert.That(rule, Does.Contain("max-width: 480px"));
        Assert.That(rule, Does.Contain("height: 320px"),
            "a fixed height, so a tall or a wide photo takes the same room");
        Assert.That(rule, Does.Contain("object-fit: cover"), "cropped to the box, never stretched");
    }

    /// <summary>Its corners are rounded.</summary>
    [Test]
    public void TheRecipePhoto_HasRoundedCorners() =>
        Assert.That(RuleOf(".markdown img:not(td img)"), Does.Match(@"border-radius:\s*1\d+px"));
}
