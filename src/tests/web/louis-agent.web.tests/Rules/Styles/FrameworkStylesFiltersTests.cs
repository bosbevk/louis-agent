using System.Text.RegularExpressions;

namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// FX166: the band's headings and summaries carried nine drop shadows in one filter (FX163's lifted words), and the
/// browser blurred all nine again on every frame a heading's shine moved, so the site lagged in every theme, flat
/// included (Louis: "take the font layers out"). A rule may cast a shadow, never a stack of them, and a heading's
/// colour no longer moves through its letters.
/// </summary>
public partial class FrameworkStylesTests
{
    /// <summary>One <c>filter:</c> declaration, up to its semicolon, over as many lines as it runs.</summary>
    [GeneratedRegex(@"filter\s*:[^;]*;", RegexOptions.NonBacktracking)]
    private static partial Regex FilterDeclaration();

    /// <summary>No framework rule chains more than two drop shadows in one filter.</summary>
    [Test]
    public void NoRule_StacksDropShadows()
    {
        var stacked = Rules()
            .SelectMany(file => FilterDeclaration().Matches(File.ReadAllText(file))
                .Where(filter => Regex.Count(filter.Value, "drop-shadow") > 2)
                .Select(_ => Path.GetFileName(file)))
            .ToList();

        Assert.That(stacked, Is.Empty);
    }

    /// <summary>
    /// A heading's colour stands still (Louis: "take the animation out of the header"): its shine swept through the
    /// letters for good, so the browser painted every heading again on every frame.
    /// </summary>
    [Test]
    public void AHeadingsWords_DoNotSweep() =>
        Assert.That(Rules().Where(file => File.ReadAllText(file).Contains("web-shine")).Select(Path.GetFileName),
            Is.Empty);
}
