using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// F169.1: the framework's rules for a page's lines and inks ask the theme, and draw each thing once, so a value a
/// theme sets is the one a visitor sees (the design review's findings 4, 6 and 11). Read from the framework's sheets,
/// the ones <see cref="FrameworkStylesTests"/> reads.
/// </summary>
public class LinesAndInksTests
{
    /// <summary>The framework's own sheets, which every site forwards.</summary>
    private static readonly string Styles = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web/Styles/web");

    /// <summary>The page-wide rule that rings whatever the keyboard is on, with the space that ends its selector.</summary>
    private const string RingRule = ":focus-visible ";

    /// <summary>
    /// The keyboard's ring is drawn in one place, the base sheet, whole and in the brand's violet. A later sheet that
    /// drew it again would win on every page, as the chat's 70 percent ring did.
    /// </summary>
    [Test]
    public void TheKeyboardsRing_IsDrawnOnce_InTheBaseSheet()
    {
        var drawnIn = FrameworkStylesTests.Rules()
            .Where(file => File.ReadLines(file).Any(StartsTheRingRule))
            .Select(file => Path.GetRelativePath(Styles, file).Replace('\\', '/'));

        Assert.That(drawnIn, Is.EqualTo(new[] { "base/_elements.scss" }));
    }

    /// <summary>A line of a sheet that opens the page-wide ring rule.</summary>
    private static bool StartsTheRingRule(string line) =>
        line.TrimStart().StartsWith(RingRule, StringComparison.Ordinal);
}
