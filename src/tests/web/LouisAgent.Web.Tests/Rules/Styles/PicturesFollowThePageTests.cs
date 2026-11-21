using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// F151.7: a drawn picture turns with the page because the engine's rule asks for the attribute the page writes. The
/// engine can't depend on the framework's web for one word, so it names the attribute and the two styles itself, and
/// this test, which sees both, holds them equal.
/// </summary>
public class PicturesFollowThePageTests
{
    /// <summary>The engine asks for the attribute the page's <c>&lt;html&gt;</c> carries.</summary>
    [Test]
    public void ThePicturesRule_AsksForThePagesAttribute() =>
        Assert.That(PageScheme.Attribute, Is.EqualTo(SiteLook.StyleAttribute));

    /// <summary>The engine's two styles are the words the page writes for them.</summary>
    [Test]
    public void ThePicturesStyles_AreThePagesWords() => Assert.Multiple(() =>
    {
        Assert.That(SiteStyle.Dark.Wire(), Is.EqualTo(PageScheme.Dark));
        Assert.That(SiteStyle.Light.Wire(), Is.EqualTo(PageScheme.Light));
    });
}
