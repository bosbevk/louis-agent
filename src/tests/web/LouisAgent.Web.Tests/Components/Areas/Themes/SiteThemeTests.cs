using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.Web.Tests.Components.Areas.Themes;

/// <summary>
/// F151.1: a theme is an id written into the page and its stylesheet, and a name a person reads on the switch. The
/// id is refused when it could not be written there safely, as code-review's picker refuses one.
/// </summary>
public class SiteThemeTests
{
    /// <summary>A theme keeps the id and the name it was made with.</summary>
    [Test]
    public void ATheme_KeepsItsIdAndItsName()
    {
        var theme = new SiteTheme("glass", "Glass");

        Assert.Multiple(() =>
        {
            Assert.That(theme.Id, Is.EqualTo("glass"));
            Assert.That(theme.Name, Is.EqualTo("Glass"));
        });
    }

    /// <summary>Lower-case letters, digits and hyphens make an id.</summary>
    [Test]
    public void AnIdOfLettersDigitsAndHyphens_IsTaken() =>
        Assert.That(new SiteTheme("flat-2", "Flat 2").Id, Is.EqualTo("flat-2"));

    /// <summary>An empty id, or one with anything else in it, is refused with a message that names it.</summary>
    [TestCase("")]
    [TestCase("Glass")]
    [TestCase("glass theme")]
    [TestCase("glass\"]")]
    [TestCase("../glass")]
    public void AnyOtherId_IsRefused_NamingIt(string id)
    {
        var refused = Assert.Throws<ArgumentException>(() => _ = new SiteTheme(id, "A theme"));

        Assert.That(refused!.Message, Does.Contain($"'{id}'"));
    }
}
