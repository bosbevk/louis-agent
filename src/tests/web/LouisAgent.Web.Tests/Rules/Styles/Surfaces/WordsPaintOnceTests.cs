using System.Text.RegularExpressions;

namespace LouisAgent.Web.Tests.Rules.Styles.Surfaces;

/// <summary>
/// FX166: the band's headings and summaries carried nine drop shadows in one filter (FX163's lifted words), and the
/// big headings swept a band of colour through their letters for good, so the browser blurred all nine again on every
/// frame and the site lagged in every theme, flat included (Louis: "take the font layers out", "take the animation
/// out of the header"). A rule may cast a shadow, never a stack of them, and a heading's colour stands still.
/// </summary>
public partial class WordsPaintOnceTests
{
    /// <summary>The folders whose sheets are read: the framework's web, its libraries and its themes.</summary>
    private static readonly string[] Read =
    [
        "src/web/LouisAgent.Web", "src/web/LouisAgent.Web.Accounts", "src/web/LouisAgent.Web.Evals",
        "src/web/LouisAgent.Web.Themes", "packs/web"
    ];

    /// <summary>One <c>filter:</c> declaration, up to its semicolon, over as many lines as it runs.</summary>
    /// <remarks>Matches <c>filter: drop-shadow(0 1px 2px red);</c>, but not <c>color: red;</c>.</remarks>
    [GeneratedRegex(@"filter\s*:[^;]*;", RegexOptions.NonBacktracking)]
    private static partial Regex FilterDeclaration();

    /// <summary>Every sheet that is read, the tools included, since the stack was a tool's.</summary>
    private static IEnumerable<string> Sheets() =>
        Read.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, folder), "*.scss",
                SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    /// <summary>No rule chains more than two drop shadows in one filter.</summary>
    [Test]
    public void NoRule_StacksDropShadows()
    {
        var stacked = Sheets()
            .SelectMany(file => FilterDeclaration().Matches(File.ReadAllText(file))
                .Where(filter => Regex.Count(filter.Value, "drop-shadow") > 2)
                .Select(_ => Path.GetFileName(file)))
            .ToList();

        Assert.That(stacked, Is.Empty);
    }

    /// <summary>
    /// FX196, Louis: "make it just solid ink". A big heading's words are their strongest colour, painted as plain
    /// text paints, with no colour clipped into the letters, so their edges are as crisp as any text's.
    /// </summary>
    [Test]
    public void AHeadingsWords_AreSolidInk()
    {
        string marks = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Styles/web/tools/surface/_marks.scss"));
        int start = marks.IndexOf("@mixin chrome-words", StringComparison.Ordinal);
        string mixin = marks[start..(marks.IndexOf('}', start) + 1)];

        Assert.Multiple(() =>
        {
            Assert.That(mixin, Does.Contain("color: var(--bs-emphasis-color);"));
            Assert.That(mixin, Does.Not.Contain("background-clip"));
        });
    }

    /// <summary>With the headings solid, no theme sets the colour of a band through them.</summary>
    [Test]
    public void NoTheme_SetsAHeadingsBand() =>
        Assert.That(Sheets().Concat(Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, "demos"), "*.scss",
                    SearchOption.AllDirectories))
                .Where(file => File.ReadAllText(file).Contains("--web-heading-band")).Select(Path.GetFileName),
            Is.Empty);

    /// <summary>A heading's colour no longer moves through its letters.</summary>
    [Test]
    public void AHeadingsWords_DoNotSweep() =>
        Assert.That(Sheets().Where(file => File.ReadAllText(file).Contains("web-shine")).Select(Path.GetFileName),
            Is.Empty);
}
