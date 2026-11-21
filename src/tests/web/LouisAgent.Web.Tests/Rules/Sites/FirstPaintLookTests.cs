using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.Web.Tests.Rules.Sites;

/// <summary>
/// F151.1: before the first paint, the script every site lists in its head applies the look the visitor picked, so a
/// returning visitor never sees the default flash by. Read from the script, with the words it must use taken from
/// <see cref="SiteLook"/> and <see cref="SiteStyle"/>, so the browser side and the C# side cannot disagree.
/// </summary>
public class FirstPaintLookTests
{
    /// <summary>The script, its line ends folded.</summary>
    private static string Script => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/web/LouisAgent.Web/wwwroot/js/first-paint.js")).ReplaceLineEndings("\n");

    /// <summary>It reads the two words the browser keeps the pick under.</summary>
    [Test]
    public void ItReadsTheTwoRememberedWords()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Script, Does.Contain($"localStorage.getItem(\"{SiteLook.ThemeKey}\")"));
            Assert.That(Script, Does.Contain($"localStorage.getItem(\"{SiteLook.StyleKey}\")"));
        });
    }

    /// <summary>It writes the two attributes on the page's root element.</summary>
    [Test]
    public void ItWritesTheTwoAttributes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Script, Does.Contain($"document.documentElement.setAttribute(\"{SiteLook.ThemeAttribute}\""));
            Assert.That(Script, Does.Contain($"document.documentElement.setAttribute(\"{SiteLook.StyleAttribute}\""));
        });
    }

    /// <summary>A remembered style is set only when it is one of the two styles' words.</summary>
    [Test]
    public void AStyle_IsSetOnlyWhenItIsOneOfTheTwo()
    {
        string words = string.Join(", ", SettingChoice.Names<SiteStyle>().Select(word => $"\"{word}\""));

        Assert.That(Script, Does.Contain($"[{words}].includes(style)"));
    }

    /// <summary>
    /// A remembered theme is set only when its id is lower-case letters, digits and hyphens, as a theme's id is, and
    /// nothing remembered is no id.
    /// </summary>
    [Test]
    public void ATheme_IsSetOnlyWhenItsIdIsLettersDigitsAndHyphens() =>
        Assert.That(Script, Does.Contain("/^[a-z0-9-]+$/.test(theme ?? \"\")"));
}
