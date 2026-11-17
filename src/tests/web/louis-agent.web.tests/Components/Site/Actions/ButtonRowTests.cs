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

    /// <summary>
    /// FX180: asked for columns, the buttons stand in that many even columns, so a long one no longer drops to a line
    /// of its own and leaves a gap beside it (Louis: "2 columns buttons"); on a phone they stack, one to a line. Not
    /// asked, a row keeps each button at its own width.
    /// </summary>
    [Test]
    public void TheButtons_StandInEvenColumns_WhenAsked_AndStackOnAPhone()
    {
        using var ctx = new BunitContext();
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Site/Actions/ButtonRow.razor.scss"));
        int phone = styles.IndexOf("@media (max-width: 600px)", StringComparison.Ordinal);

        var plain = ctx.Render<ButtonRow>(p => p.AddChildContent("<a class=\"btn\">One</a>")).Find(".button-row");
        var even = ctx.Render<ButtonRow>(p => p.Add(r => r.Columns, 2).AddChildContent("<a class=\"btn\">One</a>"))
            .Find(".button-row");

        Assert.Multiple(() =>
        {
            Assert.That(plain.HasAttribute("data-columns"), Is.False);
            Assert.That(even.GetAttribute("style"), Is.EqualTo("--button-columns: 2"));
            Assert.That(styles, Does.Contain("grid-template-columns: repeat(var(--button-columns), minmax(0, 1fr));"));
            Assert.That(phone, Is.GreaterThan(0));
            Assert.That(styles[phone..], Does.Contain("grid-template-columns: 1fr;"));
        });
    }
}
