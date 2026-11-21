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

    /// <summary>The page-wide rule that rings whatever the keyboard is on, and the space after its selector.</summary>
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

    /// <summary>
    /// A page's topic line is written in the theme's purple, which each theme makes dark enough to read on white and
    /// light enough to read on black, not in the brand's violet, which is the same in both and fell to 3.84 to 1 in the
    /// dark.
    /// </summary>
    [Test]
    public void ATopicLine_IsWrittenInTheThemesPurple() =>
        Assert.That(File.ReadLines(Path.Combine(Styles, "base", "_elements.scss")).Single(IsTheTopicRule),
            Does.Contain("color: var(--web-purple);"));

    /// <summary>A line of a sheet that holds the topic line's rule.</summary>
    private static bool IsTheTopicRule(string line) => line.StartsWith(".topic {", StringComparison.Ordinal);

    /// <summary>A line of a sheet that opens the page-wide ring rule.</summary>
    private static bool StartsTheRingRule(string line) =>
        line.TrimStart().StartsWith(RingRule, StringComparison.Ordinal);
}
