using LouisAgent.Web.Components.Admin;

namespace LouisAgent.Web.Tests.Components.Admin;

/// <summary>
/// The admin's frame: a sidebar and a content column, with slots for what's a site's own. A slot left empty leaves no
/// gap, so a site only names what it has.
/// </summary>
public class AdminShellTests
{
    /// <summary>The brand shows in the phone's top bar and in the sidebar.</summary>
    [Test]
    public void TheBrand_ShowsInTheTopBar_AndTheSidebar()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AdminShell>(parameters => parameters
            .Add(shell => shell.Brand, "<b class=\"logo\">Brand</b>")
            .Add(shell => shell.BrandTitle, "Home"));

        Assert.That(cut.FindAll(".topbar .brand .logo"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".sidebar-top .logo"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".sidebar-top a").GetAttribute("title"), Is.EqualTo("Home"));
    }

    /// <summary>The links go in the sidebar.</summary>
    [Test]
    public void TheLinks_GoInTheSidebar()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AdminShell>(parameters => parameters
            .Add(shell => shell.Links, "<a class=\"my-link\" href=\"x\">X</a>"));

        Assert.That(cut.FindAll(".sidebar .my-link"), Has.Count.EqualTo(1));
    }

    /// <summary>The top slot comes above the page, inside the content column.</summary>
    [Test]
    public void TheTop_ComesAboveThePage()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AdminShell>(parameters => parameters
            .Add(shell => shell.Top, "<div class=\"strip\">Live</div>")
            .AddChildContent("<div class=\"page\">Page</div>"));

        var inside = cut.Find(".admin-main").Children.Select(child => child.ClassName).ToList();
        Assert.That(inside, Is.EqualTo(new[] { "strip", "page" }));
    }

    /// <summary>The way back shows in the top bar and at the foot of the sidebar, going to the same place.</summary>
    [Test]
    public void TheWayBack_ShowsTwice_ToTheSamePlace()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AdminShell>(parameters => parameters
            .Add(shell => shell.BackHref, "chat")
            .Add(shell => shell.BackLabel, "Back to chat"));

        var top = cut.Find(".topbar a.icon");
        var foot = cut.Find(".side-footer a");
        Assert.That(top.GetAttribute("href"), Is.EqualTo("chat"));
        Assert.That(top.GetAttribute("aria-label"), Is.EqualTo("Back to chat"));
        Assert.That(foot.GetAttribute("href"), Is.EqualTo("chat"));
        Assert.That(foot.TextContent, Does.Contain("Back to chat"));
    }

    /// <summary>With nothing named, the page still shows, and the empty slots leave no gaps.</summary>
    [Test]
    public void WithNothingNamed_ThePageShows_AndNoSlotLeavesAGap()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AdminShell>(parameters => parameters.AddChildContent("<div class=\"page\">Page</div>"));

        Assert.That(cut.FindAll(".admin-main .page"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".topbar .brand"), Is.Empty);
        Assert.That(cut.FindAll(".topbar a.icon"), Is.Empty);
        Assert.That(cut.FindAll(".side-footer"), Is.Empty);
    }
}
