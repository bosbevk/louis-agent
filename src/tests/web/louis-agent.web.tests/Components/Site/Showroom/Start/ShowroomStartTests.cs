using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Showroom.Start;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Start;

/// <summary>
/// F127.1, step 9: the showroom's start. A big start for the main demo (the research: "a big start screen for the main
/// one"), its kicker, the page's one h1, a line, a way in and a look at it, then a glowing cover for every demo,
/// grouped as the rail groups them.
/// </summary>
public class ShowroomStartTests
{
    private static readonly ShowroomDemos Demos = new([
        new ShowroomDemo("/bakery-demo", "A bakery", "Bread at dawn", DemoGroup.ForYourBusiness, TileAccent.Green,
            DiagramIcon.Bed),
        new ShowroomDemo("/florist-demo", "A florist", "Flowers by noon", DemoGroup.ForYourBusiness, TileAccent.Blue,
            DiagramIcon.Dining),
        new ShowroomDemo("/coder-demo", "A coder", "Fixes a bug", DemoGroup.AgentsAtWork, TileAccent.Red,
            DiagramIcon.Code),
    ]);

    /// <summary>The start featuring the florist, with the site's words and a look at its website.</summary>
    private static IRenderedComponent<ShowroomStart> Start(BunitContext ctx)
    {
        ctx.Services.AddSingleton(Demos);
        return ctx.Render<ShowroomStart>(parameters => parameters
            .Add(start => start.Featured, "/florist-demo")
            .Add(start => start.Kicker, "Start here")
            .Add(start => start.Title, "Watch the agents at work.")
            .Add(start => start.Lede, "Each demo is a real run.")
            .Add(start => start.Play, "Start with the florist")
            .Add(start => start.Preview, "<svg class=\"scene\"></svg>"));
    }

    /// <summary>
    /// The feature leads with the main demo: in its colour, the kicker, the page's one h1, the line, the way in to it,
    /// and a look at its website.
    /// </summary>
    [Test]
    public void TheFeature_LeadsWithTheMainDemo()
    {
        using var ctx = new BunitContext();

        var feature = Start(ctx).Find("section#top.start-feature");

        Assert.Multiple(() =>
        {
            Assert.That(feature.ClassList, Does.Contain("accent-blue"), "the featured demo's colour");
            Assert.That(feature.QuerySelector(".start-kicker")!.TextContent, Is.EqualTo("Start here"));
            Assert.That(feature.QuerySelector("h1")!.TextContent, Is.EqualTo("Watch the agents at work."));
            Assert.That(feature.QuerySelector(".start-lede")!.TextContent, Is.EqualTo("Each demo is a real run."));
            Assert.That(feature.QuerySelector("a.start-play")!.GetAttribute("href"), Is.EqualTo("/florist-demo"));
            Assert.That(feature.QuerySelector("a.start-play")!.TextContent, Does.Contain("Start with the florist"));
            Assert.That(feature.QuerySelector(".start-preview svg.scene"), Is.Not.Null);
        });
    }

    /// <summary>
    /// Every demo has a glowing cover, under its group in the rail's order, each in its colour with its sign, its name
    /// and its line, and the section has a heading of its own for the guide's tour.
    /// </summary>
    [Test]
    public void EveryDemo_HasACover_InItsGroup()
    {
        using var ctx = new BunitContext();

        var all = Start(ctx).Find("section#every-demo");

        var covers = all.QuerySelectorAll("a.start-cover").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(all.QuerySelector("h2")!.TextContent, Is.EqualTo("All demos"));
            Assert.That(all.QuerySelectorAll(".start-group").Select(group => group.TextContent),
                Is.EqualTo(new[] { "For your business", "Agents at work" }));
            Assert.That(covers.Select(cover => cover.GetAttribute("href")),
                Is.EqualTo(new[] { "/bakery-demo", "/florist-demo", "/coder-demo" }));
            Assert.That(covers[0].ClassList, Does.Contain("accent-green"));
            // F134.1: the sign is a line drawing, never a character a system draws its own way.
            Assert.That(all.QuerySelectorAll("a.start-cover .start-sign svg.line-icon"), Has.Count.EqualTo(3));
            Assert.That(covers[2].QuerySelector(".start-sign")!.TextContent.Trim(), Is.Empty);
            Assert.That(covers[1].QuerySelector(".start-name")!.TextContent, Is.EqualTo("A florist"));
            Assert.That(covers[1].QuerySelector(".start-line")!.TextContent, Is.EqualTo("Flowers by noon"));
        });
    }
}
