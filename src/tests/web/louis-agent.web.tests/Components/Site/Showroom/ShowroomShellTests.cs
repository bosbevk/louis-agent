using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Showroom.Shell;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site.Showroom;

/// <summary>
/// F127.1: the showroom's frame. A slim bar of its own (Louis's pick 2a), a rail of every demo grouped by what a
/// visitor comes for, each in its colour, the one on screen lit; on a phone the same demos as chips; and the whole
/// frame wears the current demo's colour, so the stage is lit in it.
/// </summary>
public class ShowroomShellTests
{
    private static readonly ShowroomDemos Demos = new([
        new ShowroomDemo("/bakery-demo", "A bakery", "Bread at dawn", DemoGroup.ForYourBusiness, TileAccent.Green,
            "B"),
        new ShowroomDemo("/florist-demo", "A florist", "Flowers by noon", DemoGroup.ForYourBusiness, TileAccent.Blue,
            "F"),
        new ShowroomDemo("/coder-demo", "A coder", "Fixes a bug", DemoGroup.AgentsAtWork, TileAccent.Red, "{ }"),
    ]);

    /// <summary>
    /// The shell on a page at <paramref name="path"/>, with the site's demos and a brand, an offer and a corner.
    /// </summary>
    private static IRenderedComponent<ShowroomShell> Shell(BunitContext ctx, string path)
    {
        ctx.Services.AddSingleton(Demos);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(path);
        return ctx.Render<ShowroomShell>(parameters => parameters
            .Add(shell => shell.Brand, "<b>Brand</b>")
            .Add(shell => shell.Offer, "<a class=\"offer\" href=\"/contact\">Ask</a>")
            .Add(shell => shell.Corner, "<div class=\"guide\">Guide</div>")
            .AddChildContent("<div class=\"page\">The demo</div>"));
    }

    /// <summary>The rail lists every demo under its group, in order, each a link in its colour with its sign.</summary>
    [Test]
    public void TheRail_ListsEveryDemo_UnderItsGroup()
    {
        using var ctx = new BunitContext();

        var rail = Shell(ctx, "/florist-demo").Find("nav.demo-rail");

        Assert.Multiple(() =>
        {
            Assert.That(rail.QuerySelectorAll(".rail-group").Select(g => g.TextContent),
                Is.EqualTo(new[] { "For your business", "Agents at work" }));
            Assert.That(rail.QuerySelectorAll("a.demo-cover").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[] { "/bakery-demo", "/florist-demo", "/coder-demo" }));
            Assert.That(rail.QuerySelector("a[href='/coder-demo']")!.ClassList, Does.Contain("accent-red"));
            Assert.That(rail.QuerySelector("a[href='/coder-demo'] .cover-sign")!.TextContent, Is.EqualTo("{ }"));
        });
    }

    /// <summary>
    /// The demo on screen is lit and marked as the current page, for a screen reader too; no other is.
    /// </summary>
    [Test]
    public void TheDemoOnScreen_IsLit_AndMarkedCurrent()
    {
        using var ctx = new BunitContext();

        var cut = Shell(ctx, "/florist-demo");

        Assert.That(cut.FindAll("[aria-current='page']").Select(a => a.GetAttribute("href")),
            Is.EqualTo(new[] { "/florist-demo", "/florist-demo" }), "in the rail and in the chips");
        Assert.That(cut.FindAll(".is-current").Count, Is.EqualTo(2));
    }

    /// <summary>
    /// The frame wears the current demo's colour, so everything in it is lit in it; a page that is no demo (the
    /// start) wears the start's colour.
    /// </summary>
    [TestCase("/florist-demo", "accent-blue")]
    [TestCase("/demos", "accent-purple")]
    public void TheFrame_WearsTheDemosColour(string path, string accent)
    {
        using var ctx = new BunitContext();

        Assert.That(Shell(ctx, path).Find(".showroom").ClassList, Does.Contain(accent));
    }

    /// <summary>
    /// The slim bar: the brand home, the showroom's name, the way back to the site and the site's one offer; then the
    /// page, and the corner.
    /// </summary>
    [Test]
    public void TheBar_HasTheBrand_TheWayBack_AndTheOffer()
    {
        using var ctx = new BunitContext();

        var cut = Shell(ctx, "/bakery-demo");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".showroom-bar a.showroom-brand").GetAttribute("href"), Is.EqualTo("/"));
            Assert.That(cut.Find(".showroom-bar .showroom-pill").TextContent, Is.EqualTo("Demos"));
            Assert.That(cut.Find(".showroom-bar a.showroom-back").TextContent, Does.Contain("Back to the site"));
            Assert.That(cut.Find(".showroom-bar a.offer").GetAttribute("href"), Is.EqualTo("/contact"));
            Assert.That(cut.Find("main.showroom-stage .page").TextContent, Is.EqualTo("The demo"));
            Assert.That(cut.Find(".showroom-corner .guide").TextContent, Is.EqualTo("Guide"));
        });
    }

    /// <summary>
    /// The corner stands beside the lit frame, not in it (F127.1, Louis's screenshot of a tour's last stop): the frame is
    /// a layer of its own for its grid, and a corner inside it went under the guide's spotlight, dimmed with the page.
    /// </summary>
    [Test]
    public void TheCorner_StandsBesideTheFrame()
    {
        using var ctx = new BunitContext();

        var corner = Shell(ctx, "/bakery-demo").Find(".showroom-corner");

        Assert.That(corner.Closest(".showroom"), Is.Null);
    }

    /// <summary>On a phone the same demos are chips, in the site's order, each in its colour.</summary>
    [Test]
    public void ThePhonesChips_AreTheSameDemos()
    {
        using var ctx = new BunitContext();

        var chips = Shell(ctx, "/bakery-demo").FindAll("nav.demo-chips a.demo-chip");

        Assert.That(chips.Select(c => (c.GetAttribute("href"), c.ClassList.Contains("accent-green"))),
            Is.EqualTo(new[] { ("/bakery-demo", true), ("/florist-demo", false), ("/coder-demo", false), }));
    }

    /// <summary>A site's own words replace the framework's English.</summary>
    [Test]
    public void ASitesWords_ReplaceTheEnglish()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ShowroomText
        {
            Demos = "Demos!",
            ForYourBusiness = "Vir jou besigheid"
        });

        var cut = Shell(ctx, "/bakery-demo");

        Assert.That((cut.Find(".showroom-pill").TextContent, cut.Find(".rail-group").TextContent),
            Is.EqualTo(("Demos!", "Vir jou besigheid")));
    }
}
