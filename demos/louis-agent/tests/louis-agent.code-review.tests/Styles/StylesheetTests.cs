namespace LouisAgent.CodeReview.Tests.Styles;

/// <summary>
/// The code-review app's stylesheet is Sass: <c>app.scss</c> lists the framework's rules and its packs', and its own few
/// after them, so it keeps no copy. What's checked is what the build really made, its themes included.
/// </summary>
public class StylesheetTests
{
    /// <summary>The code-review app's stylesheet.</summary>
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.code-review");

    /// <summary>Every partial of its own is on the list, and everything on the list is there.</summary>
    [Test]
    public void EveryPartial_IsOnTheList_AndEveryNameOnTheListIsThere() =>
        Assert.That(Css.Forwards().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>It lists the framework's rules rather than keeping a copy, colours first.</summary>
    [Test]
    public void ItListsTheFrameworksRules_ColoursFirst()
    {
        Assert.That(Css.FrameworkForwards()[0], Is.EqualTo("web/settings/colours"));
        Assert.That(Css.FrameworkForwards(), Does.Contain("web/layout/shell").And.Contain("web/admin/screens"));
        Assert.That(Css.FrameworkForwards().Where(partial => !File.Exists(Css.FrameworkPartial(partial))), Is.Empty);
    }

    /// <summary>The git and file rules come from their packs' web projects, so the build found them.</summary>
    [Test]
    public void ThePacksRules_AreInTheBuild() =>
        Assert.That(Css.Read(), Does.Contain(".changes-panel").And.Contain(".file-tree").And.Contain(".pack-split"));

    /// <summary>Each theme the picker offers was built, and sets the framework's colour names in both forms.</summary>
    [TestCase("retro")]
    [TestCase("paper")]
    public void ATheme_IsBuilt_WithBothFormsOfItsColours(string theme)
    {
        var built = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/web/louis-agent.code-review/wwwroot/css/themes", $"{theme}.css"));

        Assert.That(built, Does.Contain("--bs-primary:").And.Contain("--bs-primary-rgb:").And.Contain("--bs-body-bg:"));
    }
}
