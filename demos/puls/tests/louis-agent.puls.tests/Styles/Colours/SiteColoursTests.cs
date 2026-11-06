using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests.Styles.Colours;

/// <summary>
/// The framework's colours come first and Puls's after them, so the same name set twice gives Puls's. Puls has to set
/// every name the framework does, or a framework colour would show through and change how Puls looks; and every colour
/// the shared rules read needs a framework value, or a site with no colours of its own would get nothing there.
/// </summary>
public partial class SiteColoursTests
{
    /// <summary>What Bootstrap sets itself the same on every site, so the framework has no need to.</summary>
    private static readonly string[] BootstrapsOwn =
        ["--bs-black", "--bs-black-rgb", "--bs-white", "--bs-border-radius-xl"];

    /// <summary>One variable being set: matches <c>--bs-primary: #ffffff;</c> and gives its name.</summary>
    [GeneratedRegex(@"(--[a-z0-9-]+):", RegexOptions.NonBacktracking)]
    private static partial Regex Sets();

    /// <summary>One colour being read: matches <c>var(--bs-primary)</c> and gives its name.</summary>
    [GeneratedRegex(@"var\((--(?:bs|web)-[a-z0-9-]+)", RegexOptions.NonBacktracking)]
    private static partial Regex Reads();

    private static SassStylesheet Css => SassStylesheet.Of("louis-agent.puls");

    /// <summary>The two blocks of colours in the CSS: the framework's first, then the site's.</summary>
    private static (string Framework, string Site) Blocks()
    {
        string css = Css.Read();
        int first = css.IndexOf(":root {", StringComparison.Ordinal);
        int second = css.IndexOf(":root {", first + 1, StringComparison.Ordinal);
        Assert.That(second, Is.GreaterThan(first), "the framework's colours and then the site's");
        return (Block(css, first), Block(css, second));
    }

    /// <summary>One <c>:root</c> block, to its closing brace.</summary>
    private static string Block(string css, int start) => css[start..css.IndexOf('}', start)];

    private static IEnumerable<string> Names(string block) => Sets().Matches(block).Select(m => m.Groups[1].Value);

    /// <summary>The framework's colours are first on the list, so a site's win.</summary>
    [Test]
    public void TheFrameworksColours_AreFirstOnTheList() =>
        Assert.That(Css.FrameworkForwards().First(), Is.EqualTo("web/settings/colours"));

    /// <summary>Puls sets every name the framework does, so none of the framework's colours shows through.</summary>
    [Test]
    public void Puls_SetsEveryNameTheFrameworkDoes()
    {
        var (framework, site) = Blocks();

        Assert.That(Names(framework).Except(Names(site)), Is.Empty);
    }

    /// <summary>Every colour the shared rules read has a framework value.</summary>
    [Test]
    public void EveryColourTheSharedRulesRead_HasAFrameworkValue()
    {
        string[] shared = ["base/", "layout/", "chat/", "admin/"];
        var read = Css.AreaFiles()
            .Where(file => shared.Any(folder => file.StartsWith(folder, StringComparison.Ordinal)))
            .SelectMany(file => Reads().Matches(Css.Source(file)).Select(m => m.Groups[1].Value)).Distinct();

        Assert.That(read.Except(Names(Blocks().Framework)).Except(BootstrapsOwn), Is.Empty);
    }
}
