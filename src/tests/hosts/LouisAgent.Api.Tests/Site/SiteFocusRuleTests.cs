using LouisAgent.Composition.Sites;

namespace LouisAgent.Api.Tests.Site;

/// <summary>
/// F126.3.3: the site's API asks the site's focus rule itself, so a page it reads carries the same focus the widget
/// would send. The rule is the shared contract's, which the API reaches without the web.
/// </summary>
public class SiteFocusRuleTests
{
    /// <summary>The API can make a site's focus rule and ask it the focus of an address.</summary>
    [Test]
    public void TheApi_AsksASitesFocusRule_TheFocusOfAnAddress()
    {
        var focuses = new SiteFocuses("selling", [new FocusPart("/demos", "demos")]);

        Assert.That(focuses.Of("/demos/kitchen"), Is.EqualTo("demos"));
        Assert.That(focuses.Of("/"), Is.EqualTo("selling"));
    }
}
