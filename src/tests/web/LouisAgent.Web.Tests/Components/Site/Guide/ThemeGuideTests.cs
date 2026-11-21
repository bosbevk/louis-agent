using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Guide;

namespace LouisAgent.Web.Tests.Components.Site.Guide;

/// <summary>
/// The theme's guide shows every control working (Louis, 2026-11-18: "make sure every control works"): a button
/// pressed says so, and nothing in it leads away to an address, since the framework names no site's pages.
/// </summary>
public class ThemeGuideTests
{
    /// <summary>The guide on a site with the framework's services and no theme of its own.</summary>
    private static IRenderedComponent<ThemeGuide> Render(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSiteAreas(_ => { });
        return ctx.Render<ThemeGuide>();
    }

    /// <summary>Both live buttons answer a press, and the guide holds no link at all.</summary>
    [Test]
    public void ItsButtons_AnswerAPress_AndNothingLeadsAway()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);
        cut.Find("button.btn-fill:not([disabled])").Click();
        cut.Find("button.btn-line:not([disabled])").Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".guide-pressed").TextContent.Trim(), Is.EqualTo("Pressed 2 times"));
            Assert.That(cut.FindAll(".theme-guide a"), Is.Empty);
        });
    }

    /// <summary>
    /// What a visitor reads is plain: no colon or semicolon, and no name of code such as a component's.
    /// </summary>
    [Test]
    public void ItsWords_ArePlain()
    {
        using var ctx = new BunitContext();
        string words = Render(ctx).Find(".theme-guide").TextContent;

        Assert.Multiple(() =>
        {
            Assert.That(words, Does.Not.Contain(":").And.Not.Contain(";"));
            Assert.That(words, Does.Not.Contain("LookSwitch").And.Not.Contain("MarkStates").And.Not.Contain("Tile is")
                .And.Not.Contain("LouisAgent.Core").And.Not.Contain("ShowCard"));
        });
    }

    /// <summary>A control that can't be used says so in words, so it doesn't look broken.</summary>
    [Test]
    public void AnOffControl_SaysItIsOff()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        Assert.That(cut.FindAll("button[disabled]").Where(button => button.ClassList.Contains("btn"))
            .Select(button => button.TextContent.Trim()), Has.All.EqualTo("Can't press"));
    }

    /// <summary>
    /// Each kind of control stands once (Louis, 2026-11-18, of two switches side by side: "seems like a duplicate"):
    /// one switch to flip and one that is locked, and the one the focus card tabs to.
    /// </summary>
    [Test]
    public void EachSwitch_StandsOnce()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.That(cut.FindAll("button.switch").Select(button => button.GetAttribute("aria-label")),
            Is.EqualTo(new[] { "A switch", "A locked switch" }));
    }

    /// <summary>
    /// Nothing on the guide is shown twice (Louis, 2026-11-18: "check all these for duplicates, redesign if
    /// necessary"): four cards, one off button, and one status pill, the one that answers the buttons. The focus ring
    /// is said on the buttons' card, since every control wears it.
    /// </summary>
    [Test]
    public void NothingIsShownTwice()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".guide-grid .panel h2, .guide-grid .panel h3").Select(title => title.TextContent),
                Is.EqualTo(new[] { "Buttons", "Switch and slider", "Field", "Tags" }));
            Assert.That(cut.FindAll("button.btn[disabled]"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".status-pill"), Has.Count.EqualTo(1));
            Assert.That(cut.Find(".guide-grid .panel").TextContent, Does.Contain("Press Tab"));
        });
    }
}
