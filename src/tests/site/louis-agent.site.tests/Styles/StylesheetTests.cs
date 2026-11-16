namespace LouisAgent.Site.Tests.Styles;

/// <summary>
/// The framework's site's stylesheet is Sass: <c>app.scss</c> lists the framework's rules, the two themes and the
/// triage pack's demo, so it keeps no copy (F124), and nothing of the site's own (FX162). What's checked is what the
/// build really made.
/// </summary>
public class StylesheetTests
{
    /// <summary>The site's stylesheet.</summary>
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.site");

    /// <summary>
    /// The site decides no look of its own (FX162, Louis: "remove all custom css from .site we should just use the
    /// theme", "I need all custom css out immediately"): it lists no partial of its own, and no component of it has
    /// a sheet beside it.
    /// </summary>
    [Test]
    public void TheSite_HasNoStylesOfItsOwn()
    {
        string site = Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site");
        var sheets = Directory.EnumerateFiles(site, "*.scss", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(path => Path.GetRelativePath(site, path).Replace('\\', '/'))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(Css.Listed().Where(partial => partial.StartsWith("site/")), Is.Empty);
            Assert.That(sheets, Is.EqualTo(new[] { "Styles/app.scss" }));
        });
    }

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
    /// The build holds each theme's values for each style, so the page's pick has something to turn on.
    /// </summary>
    [TestCase("glass", "dark")]
    [TestCase("glass", "light")]
    [TestCase("flat", "dark")]
    [TestCase("flat", "light")]
    public void TheBuild_HoldsEachThemesValues(string theme, string style) =>
        Assert.That(Css.Read(), Does.Contain($":root[data-theme={theme}][data-style={style}]"));

    /// <summary>The triage demo's rules come from the triage pack's web project, so the build found them.</summary>
    [Test]
    public void ThePacksRules_AreInTheBuild() =>
        Assert.That(Css.Read(), Does.Contain(".step-popup").And.Contain(".popup-kind"));
}
