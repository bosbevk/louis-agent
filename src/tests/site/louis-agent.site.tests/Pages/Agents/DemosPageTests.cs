using Microsoft.AspNetCore.Components;
using LouisAgent.Site.Layout;
using LouisAgent.Site.Pages.Agents;

namespace LouisAgent.Site.Tests.Pages.Agents;

/// <summary>
/// The demo index (F122, Louis: "a button there called demos", "which takes you to the demo index"). Since F134.1 it
/// is the showroom's start as a stage: a window for every demo, the guesthouse forward first (Louis: "make the
/// guesthouse the main one"), and a dock with a button per demo.
/// </summary>
public class DemosPageTests
{
    /// <summary>
    /// The page is at /demos, in the showroom. The guesthouse is forward, so the way in leads to it. Every demo has
    /// its window and its button on the dock, the businesses first.
    /// </summary>
    [Test]
    public void ThePage_PutsTheGuesthouseForward_AndHasAWindowForEachDemo()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<DemosPage>();

        var route = typeof(DemosPage).GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>()
            .Single().Template;
        Assert.Multiple(() =>
        {
            Assert.That(route, Is.EqualTo("/demos"));
            Assert.That(typeof(DemosPage).GetCustomAttributes(typeof(LayoutAttribute), false)
                .Cast<LayoutAttribute>().Single().LayoutType, Is.EqualTo(typeof(ShowroomLayout)));
            Assert.That(cut.Find("#top a.start-play").GetAttribute("href"), Is.EqualTo("/guesthouse-demo"));
            Assert.That(cut.Find("#top .start-caption h2").TextContent, Is.EqualTo("A guesthouse"));
            Assert.That(cut.FindAll("h1"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("#top .deck-item .framed-window"), Has.Count.EqualTo(5));
            Assert.That(cut.FindAll("#top nav.dock button").Select(button => button.Id),
                Is.EqualTo(new[]
                {
                    "guesthouse-demo", "restaurant-demo", "law-practice-demo", "coding-agent", "triage-demo"
                }));
            // The rule is for words a visitor reads. The deck's own style attributes are not words.
            Assert.That(cut.Find("#top").TextContent, Does.Not.Contain(";"), "no semicolons in the site's words");
        });
    }
}
