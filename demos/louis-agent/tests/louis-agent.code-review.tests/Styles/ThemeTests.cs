using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.CodeReview.Tests.Styles;

/// <summary>
/// The code-review app's own stylesheet (F124: the framework's site and its look left for src/site): the themes it
/// offers, the framework's two and its own retro (F151.5), and the files and git packs' rules for its admin screens.
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
    /// The app offers the framework's two themes and then its own, so glass is what a first visit gets.
    /// </summary>
    [Test]
    public void TheApp_OffersGlassFlatThenRetro()
    {
        var areas = new ServiceCollection().AddSiteAreas(CodeReviewAreas.Name).BuildServiceProvider()
            .GetRequiredService<SiteAreas>();

        Assert.That(areas.Themes.Select(theme => theme.Id), Is.EqualTo(new[] { "glass", "flat", "retro" }));
    }

    /// <summary>
    /// Retro is held to the names as glass and flat are: in each style it leaves none of the framework's names unset,
    /// so no other theme's value shows through when the page wears it. The sign-in providers' colours are theirs,
    /// not a theme's, so they are not among the names.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void Retro_SetsEveryNameTheFrameworkDoes_InEachStyle(SiteStyle style)
    {
        string css = Css.Read();
        var providers = ThemeSheet.NamesToSet(File.ReadAllText(Css.FrameworkPartial(ProvidersColours)));
        var names = ThemeSheet.NamesToSet(css).Except(providers).ToList();
        Assert.That(names, Has.Count.GreaterThan(50), "the framework's names were read");

        Assert.That(ThemeSheet.Missing(css, RetroTheme.Theme, style, names), Is.Empty);
    }

    /// <summary>
    /// A theme is values and holds no rule for a component (ADR 0124), so each of retro's sheets is one block, the
    /// one for its theme and style. Its terminal dressing is the app's own sheet beside them.
    /// </summary>
    [TestCase("retro/dark", "dark")]
    [TestCase("retro/light", "light")]
    public void RetrosSheet_IsOneBlockOfValues(string partial, string style)
    {
        string sheet = Css.Source(partial);

        Assert.Multiple(() =>
        {
            Assert.That(sheet, Does.Contain($":root[data-theme=\"retro\"][data-style=\"{style}\"] {{"));
            Assert.That(sheet.Count(letter => letter == OpensABlock), Is.EqualTo(1), "one block, and no rule beside it");
        });
    }

    /// <summary>
    /// Retro's switch-on lets go of the app when it ends. Held, the app frames its off-screen panels, and a phone's
    /// page scrolls sideways to them.
    /// </summary>
    [Test]
    public void RetrosSwitchOn_LetsGoOfTheApp_WhenItEnds() =>
        Assert.That(Css.Source("retro/terminal"), Does.Contain("animation: crt-on .55s var(--web-ease) backwards;"));

    /// <summary>The framework's partial that holds the sign-in providers' own colours.</summary>
    private const string ProvidersColours = "web/settings/brands";

    /// <summary>What opens a block in a stylesheet.</summary>
    private const char OpensABlock = '{';
}
