using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles;

// FX24 (Louis, 2026-10-26: "make the image a fixed size to fit in our chat screen", "give it some rounded corners"): the recipe's photo is a fixed box with rounded
// corners, not the page's full-size picture (a 1024-wide photo filled the chat). The product photos in a shopping list's cells keep their own, smaller size. The
// stylesheet is what sizes it, so this holds the rule in the file; how it looks is checked in the browser.
public partial class RecipePhotoStyleTests
{
    private static string Css => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src", "louis-agent.puls", "wwwroot", "css", "app.css"));

    private static string RuleOf(string selector)
    {
        var match = Regex.Match(Css, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(match.Success, Is.True, $"no rule for {selector}");
        return match.Groups[1].Value;
    }

    [Test]
    public void TheRecipePhoto_IsAFixedBox_ThatFitsTheChat()
    {
        string rule = RuleOf(".markdown img:not(td img)");

        Assert.That(rule, Does.Contain("width: 100%"), "never wider than the chat");
        Assert.That(rule, Does.Contain("max-width: 480px"));
        Assert.That(rule, Does.Contain("height: 320px"), "a fixed height, so a tall or a wide photo takes the same room");
        Assert.That(rule, Does.Contain("object-fit: cover"), "cropped to the box, never stretched");
    }

    [Test]
    public void TheRecipePhoto_HasRoundedCorners() =>
        Assert.That(RuleOf(".markdown img:not(td img)"), Does.Match(@"border-radius:\s*1\d+px"));
}
