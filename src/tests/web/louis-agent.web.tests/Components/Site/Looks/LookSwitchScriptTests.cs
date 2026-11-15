using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.Web.Tests.Components.Site.Looks;

/// <summary>
/// F151.3: the switch's half in the browser writes the pick on the page and remembers it, and follows a pick made in
/// another tab. Read from the module, with the words it must use taken from <see cref="SiteLook"/> and
/// <see cref="SiteStyle"/>, so the browser side and the C# side cannot disagree.
/// </summary>
public class LookSwitchScriptTests
{
    /// <summary>The module, its line ends folded.</summary>
    private static string Module => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/web/louis-agent.web/Components/Site/Looks/LookSwitch.razor.js")).ReplaceLineEndings("\n");

    /// <summary>It remembers under the two words the first paint reads.</summary>
    [Test]
    public void ItUsesTheTwoRememberedWords()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain($"\"{SiteLook.ThemeKey}\""));
            Assert.That(Module, Does.Contain($"\"{SiteLook.StyleKey}\""));
        });
    }

    /// <summary>It writes the two attributes on the page's root element.</summary>
    [Test]
    public void ItWritesTheTwoAttributes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain($"\"{SiteLook.ThemeAttribute}\""));
            Assert.That(Module, Does.Contain($"\"{SiteLook.StyleAttribute}\""));
        });
    }

    /// <summary>A style is set only when it is one of the two styles' words.</summary>
    [Test]
    public void AStyle_IsSetOnlyWhenItIsOneOfTheTwo()
    {
        string words = string.Join(", ", SettingChoice.Names<SiteStyle>().Select(word => $"\"{word}\""));

        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain($"const styles = [{words}];"));
            Assert.That(Module, Does.Contain("styles.includes("));
        });
    }

    /// <summary>A theme is set only when its id is lower-case letters, digits and hyphens, as a theme's is.</summary>
    [Test]
    public void ATheme_IsSetOnlyWhenItsIdCouldBeATheme() =>
        Assert.That(Module, Does.Contain("/^[a-z0-9-]+$/.test("));

    /// <summary>It follows a pick made in another tab, which the browser tells it of with the storage event.</summary>
    [Test]
    public void ItFollowsAnotherTab() =>
        Assert.That(Module, Does.Contain("addEventListener(\"storage\""));
}
