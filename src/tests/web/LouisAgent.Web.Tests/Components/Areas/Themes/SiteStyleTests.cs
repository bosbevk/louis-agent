using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.Web.Tests.Components.Areas.Themes;

/// <summary>
/// F151.1: a style is light or dark, read once from the word the browser keeps and written back the same, and a
/// first visit is light.
/// </summary>
public class SiteStyleTests
{
    /// <summary>The two words read as the two styles.</summary>
    [TestCase("light", SiteStyle.Light)]
    [TestCase("dark", SiteStyle.Dark)]
    public void TheTwoWords_ReadAsTheTwoStyles(string word, SiteStyle style)
    {
        Assert.Multiple(() =>
        {
            Assert.That(SettingChoice.TryParse<SiteStyle>(word, out var read), Is.True);
            Assert.That(read, Is.EqualTo(style));
        });
    }

    /// <summary>A style is written back as the word it was read from.</summary>
    [TestCase(SiteStyle.Light, "light")]
    [TestCase(SiteStyle.Dark, "dark")]
    public void AStyle_IsWrittenAsItsWord(SiteStyle style, string word) =>
        Assert.That(style.Wire(), Is.EqualTo(word));

    /// <summary>Any other word is not a style.</summary>
    [TestCase("dim")]
    [TestCase("auto")]
    [TestCase("1")]
    public void AnyOtherWord_IsNotAStyle(string word) =>
        Assert.That(SettingChoice.TryParse<SiteStyle>(word, out _), Is.False);

    /// <summary>A first visit, with nothing remembered, is light.</summary>
    [Test]
    public void AFirstVisit_IsLight() =>
        Assert.That(SiteLook.DefaultStyle, Is.EqualTo(SiteStyle.Light));
}
