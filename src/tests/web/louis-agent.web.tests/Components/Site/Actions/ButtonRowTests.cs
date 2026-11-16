using LouisAgent.Web.Components.Site.Actions;

namespace LouisAgent.Web.Tests.Components.Site.Actions;

/// <summary>
/// A row of the theme's buttons, side by side (FX163, the last band's two ways on): a site hands it its buttons, and
/// a band, which stacks what it holds, shows them in one row.
/// </summary>
public class ButtonRowTests
{
    /// <summary>The buttons it is given stand in one row, in order.</summary>
    [Test]
    public void TheRow_HoldsItsButtons_InOrder()
    {
        using var ctx = new BunitContext();

        var row = ctx.Render<ButtonRow>(p => p.AddChildContent(
            "<button class=\"btn\">One</button><a class=\"btn\" href=\"/two\">Two</a>")).Find(".button-row");

        Assert.That(row.Children.Select(c => c.TextContent), Is.EqualTo(new[] { "One", "Two" }));
    }
}
