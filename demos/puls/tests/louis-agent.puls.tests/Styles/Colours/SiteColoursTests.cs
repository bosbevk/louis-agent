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
        ["--bs-black", "--bs-black-rgb", "--bs-white", "--bs-border-radius", "--bs-border-radius-xl"];

    /// <summary>One variable being set: matches <c>--bs-primary: #ffffff;</c> and gives its name.</summary>
    [GeneratedRegex(@"(--[a-z0-9-]+):", RegexOptions.NonBacktracking)]
    private static partial Regex Sets();

    /// <summary>One colour being read: matches <c>var(--bs-primary)</c> and gives its name.</summary>
    [GeneratedRegex(@"var\((--(?:bs|web)-[a-z0-9-]+)", RegexOptions.NonBacktracking)]
    private static partial Regex Reads();

    private static SassStylesheet Css => SassStylesheet.Of("louis-agent.puls");

    /// <summary>What opens a block of names in the CSS.</summary>
    private const string Root = ":root {";

    /// <summary>
    /// The blocks of names in the CSS (F151.1, Louis's pick a of three): one for each of the framework's settings
    /// sheets the list names before Puls's own, its colours first, then Puls's. They are counted from the list, so a
    /// settings sheet the framework adds moves nothing.
    /// </summary>
    private static (IReadOnlyList<string> Framework, string Site) Blocks()
    {
        int frameworks = Css.Listed()
            .TakeWhile(name => name.StartsWith("web/", StringComparison.Ordinal))
            .Count(name => name.StartsWith("web/settings/", StringComparison.Ordinal));

        string css = Css.Read();
        var starts = Starts(css).Take(frameworks + 1).ToList();
        Assert.That(starts, Has.Count.EqualTo(frameworks + 1), "the framework's blocks and then the site's");
        return ([.. starts.Take(frameworks).Select(start => Block(css, start))], Block(css, starts[^1]));
    }

    /// <summary>Where each block of names opens, in the order the CSS has them.</summary>
    private static IEnumerable<int> Starts(string css)
    {
        for (int at = css.IndexOf(Root, StringComparison.Ordinal);
             at >= 0;
             at = css.IndexOf(Root, at + 1, StringComparison.Ordinal))
            yield return at;
    }

    /// <summary>One <c>:root</c> block, to its closing brace.</summary>
    private static string Block(string css, int start) => css[start..css.IndexOf('}', start)];

    private static IEnumerable<string> Names(string block) => Sets().Matches(block).Select(m => m.Groups[1].Value);

    /// <summary>The framework's colours are first on the list, so a site's win.</summary>
    [Test]
    public void TheFrameworksColours_AreFirstOnTheList() =>
        Assert.That(Css.FrameworkForwards().First(), Is.EqualTo("web/settings/colours"));

    /// <summary>
    /// Puls sets every colour the framework does, so none of the framework's colours shows through. The surfaces
    /// (F151.1) are not asked of it: each one's value is a colour name, so it follows Puls's colours.
    /// </summary>
    [Test]
    public void Puls_SetsEveryColourTheFrameworkDoes()
    {
        var (framework, site) = Blocks();

        Assert.That(Names(framework[0]).Except(Names(site)), Is.Empty);
    }

    /// <summary>Every colour the shared rules read has a framework value.</summary>
    [Test]
    public void EveryColourTheSharedRulesRead_HasAFrameworkValue()
    {
        string[] shared = ["base/", "layout/", "chat/", "admin/"];
        var own = Css.AreaFiles()
            .Where(file => shared.Any(folder => file.StartsWith(folder, StringComparison.Ordinal)))
            .Select(Css.Source);
        var frameworks = Css.FrameworkForwards().Select(partial => File.ReadAllText(Css.FrameworkPartial(partial)));
        var read = own.Concat(frameworks)
            .SelectMany(text => Reads().Matches(text).Select(m => m.Groups[1].Value)).Distinct();

        Assert.That(read.Except(Blocks().Framework.SelectMany(Names)).Except(BootstrapsOwn), Is.Empty);
    }
}
