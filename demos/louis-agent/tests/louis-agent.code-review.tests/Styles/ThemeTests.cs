using System.Text.RegularExpressions;

namespace LouisAgent.CodeReview.Tests.Styles;

/// <summary>
/// The code-review app's own stylesheet (F124: the framework's site and its look left for src/site): its themes, which
/// the chat's picker switches between, and the files and git packs' rules for its admin screens.
/// </summary>
public class ThemeTests
{
    /// <summary>The code-review app's stylesheet.</summary>
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.code-review");

    /// <summary>Every partial of its own is on the list, and everything on the list is there.</summary>
    [Test]
    public void EveryPartial_IsOnTheList_AndEveryNameOnTheListIsThere() =>
        Assert.That(Css.Forwards().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>The git and file rules come from their packs' web projects, so the build found them.</summary>
    [Test]
    public void ThePacksRules_AreInTheBuild() =>
        Assert.That(Css.Read(), Does.Contain(".changes-panel").And.Contain(".file-tree").And.Contain(".pack-split"));

    /// <summary>
    /// Each theme the picker offers was built, and sets the framework's colour names in both forms. Console is the
    /// framework's colours as they are; retro is the one to switch to.
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
}
