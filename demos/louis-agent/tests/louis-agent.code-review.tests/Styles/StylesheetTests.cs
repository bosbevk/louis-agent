using System.Text.RegularExpressions;

namespace LouisAgent.CodeReview.Tests.Styles;

/// <summary>
/// The code-review app's stylesheet is Sass: <c>app.scss</c> lists the framework's rules and its packs', then the
/// site's look (F109.1) and its own few, so it keeps no copy. What's checked is what the build really made, its
/// themes included.
/// </summary>
public class StylesheetTests
{
    /// <summary>The code-review app's stylesheet.</summary>
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.code-review");

    /// <summary>The site's three film partials, where every film keyframe lives.</summary>
    private static readonly string[] Films = ["site/movie/stage", "site/movie/short", "site/movie/detail"];

    /// <summary>Every partial of the site's look.</summary>
    private static readonly string[] Site =
    [
        "site/settings/tokens", "site/base/elements", "site/landing/hero", "site/landing/live-turn",
        "site/landing/sections", "site/layout/responsive", .. Films
    ];

    /// <summary>Every partial of its own is on the list, and everything on the list is there.</summary>
    [Test]
    public void EveryPartial_IsOnTheList_AndEveryNameOnTheListIsThere() =>
        Assert.That(Css.Forwards().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>It lists the framework's rules rather than keeping a copy, colours first.</summary>
    [Test]
    public void ItListsTheFrameworksRules_ColoursFirst()
    {
        Assert.That(Css.FrameworkForwards()[0], Is.EqualTo("web/settings/colours"));
        Assert.That(Css.FrameworkForwards(), Does.Contain("web/layout/shell").And.Contain("web/base/elements"));
        Assert.That(Css.FrameworkForwards().Where(partial => !File.Exists(Css.FrameworkPartial(partial))), Is.Empty);
    }

    /// <summary>
    /// The site's values come right after the framework's colours, and its plain elements right after the
    /// framework's, so each builds on the one before and the site's win where they meet.
    /// </summary>
    [Test]
    public void TheSitesValuesAndElements_ComeRightAfterTheFrameworks()
    {
        string list = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/web/louis-agent.code-review/Styles/app.scss"));
        var forwards = Regex.Matches(list, "@forward \"([^\"]+)\"").Select(match => match.Groups[1].Value);

        Assert.That(forwards.Take(4),
            Is.EqualTo(new[]
            {
                "web/settings/colours", "site/settings/tokens", "web/base/elements", "site/base/elements"
            }));
    }

    /// <summary>
    /// The site's look stays inside the site's frame: its element rules hang off the frame's root class, so the chat
    /// and the admin keep the framework's. Only the frame's values sit on the root.
    /// </summary>
    [Test]
    public void TheSitesLook_StaysInsideTheFrame()
    {
        var topLevel = Css.Source("site/base/elements").Split('\n')
            .Where(line => line.Length > 0 && !char.IsWhiteSpace(line[0]) && !line.StartsWith("/*") &&
                           !line.StartsWith('*') && !line.StartsWith("//"))
            .Where(line => !line.StartsWith('}'))
            .ToList();

        Assert.That(topLevel, Is.Not.Empty, "the search found the partial's rules");
        Assert.That(topLevel, Is.All.Matches<string>(line => line.StartsWith(".site")));
        Assert.That(Css.Read(), Does.Contain(".site h1").And.Contain(".site .panel").And.Contain(".site .btn"));
    }

    /// <summary>The git and file rules come from their packs' web projects, so the build found them.</summary>
    [Test]
    public void ThePacksRules_AreInTheBuild() =>
        Assert.That(Css.Read(), Does.Contain(".changes-panel").And.Contain(".file-tree").And.Contain(".pack-split"));

    /// <summary>
    /// Each theme the picker offers was built, and sets the framework's colour names in both forms. Console is the
    /// site's look, the framework's colours as they are; retro is the one to switch to.
    /// </summary>
    [TestCase("console")]
    [TestCase("retro")]
    public void ATheme_IsBuilt_WithBothFormsOfItsColours(string theme)
    {
        var built = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/web/louis-agent.code-review/wwwroot/css/themes", $"{theme}.css"));
        Assert.That(built, Does.Contain("--bs-primary:").And.Contain("--bs-primary-rgb:").And.Contain("--bs-body-bg:"));
    }

    /// <summary>The picker lists exactly the themes that were built, console first as the default.</summary>
    [Test]
    public void ThePickersList_IsTheThemesBuilt_ConsoleFirst()
    {
        string themes = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/web/louis-agent.code-review/wwwroot/css/themes/themes.json"));
        var ids = Regex.Matches(themes, "\"id\": \"([a-z0-9-]+)\"").Select(match => match.Groups[1].Value).ToList();

        Assert.That(ids, Is.EqualTo(new[] { "console", "retro" }));
    }

    /// <summary>
    /// Every name the site's rules read is set, by the framework or by the site, so none falls through. The site's
    /// partials only: the framework's rules also read what Bootstrap and the scripts set.
    /// </summary>
    [Test]
    public void EveryNameASiteRuleReads_IsSet()
    {
        string css = Css.Read();
        var read = Site.Select(Css.Source).SelectMany(source => Regex.Matches(source, @"var\((--[a-z0-9-]+)"))
            .Select(m => m.Groups[1].Value).Distinct().ToList();
        var setByAFilm = new[] { "--loop", "--d" };

        Assert.That(read, Does.Contain("--site-gutter"), "the search found the site's names");
        Assert.That(read.Except(setByAFilm).Where(name => !css.Contains($"{name}:")), Is.Empty);
    }

    /// <summary>
    /// The two films sit in one stylesheet, so each keyframe name ends in its film's letter (<c>-s</c>, <c>-d</c>) and
    /// no name is declared twice; a shared name would make one film play the other's timing.
    /// </summary>
    [Test]
    public void EachFilm_HasItsOwnKeyframes_AndNoNameIsDeclaredTwice()
    {
        var names = Films.Select(Css.Source).SelectMany(source => source.Split("@keyframes ").Skip(1))
            .Select(part => part[..part.IndexOf(' ')]).ToList();

        Assert.That(names, Is.Not.Empty, "the search found the films' keyframes");
        Assert.That(names, Is.Unique);
        Assert.That(names.Where(n => n != "bar"), Is.All.Matches<string>(BelongsToOneFilm));
    }

    /// <summary>
    /// A PC with its animations switched off asks for reduced motion; that stills the hero's typing but not the films,
    /// which are the content and play as a video would (FX35). Every selector in the site's reduced-motion rule
    /// leaves the stage out.
    /// </summary>
    [Test]
    public void ReducedMotion_StillsTheDecoration_AndLeavesTheFilmsPlaying()
    {
        string source = Css.Source("site/settings/tokens");
        string block = source[source.IndexOf("@media (prefers-reduced-motion: reduce)")..];
        block = block[..block.IndexOf("\n}")];
        var selectors = block.Split('{').Skip(1).Select(rule => rule[(rule.LastIndexOf('}') + 1)..].Trim())
            .Where(s => s.Length > 0).SelectMany(list => list.Split(',')).Select(s => s.Trim()).ToList();

        Assert.That(selectors, Is.Not.Empty, "the reduced-motion rule is there");
        Assert.That(selectors, Is.All.Contains(":not(.stage *)"), "nothing in it reaches a film");
    }

    /// <summary>A film's keyframe name ends in <c>-s</c> or <c>-d</c>, or in that letter and the scene's number.</summary>
    private static bool BelongsToOneFilm(string name)
    {
        string tail = name[(name.LastIndexOf('-') + 1)..];
        return tail is ['s' or 'd', ..] && tail[1..].All(char.IsDigit);
    }
}
