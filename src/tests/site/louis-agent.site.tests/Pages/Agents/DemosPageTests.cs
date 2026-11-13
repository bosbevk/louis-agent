using Microsoft.AspNetCore.Components;
using LouisAgent.Site.Layout;
using LouisAgent.Site.Pages.Agents;
using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// Each demo's window has its own address under the start's (ADR 0107), the ones the sitemap lists. Opened at
    /// one, that demo is forward, with its name and its way in, so each address is its own page.
    /// </summary>
    [TestCase("/demos/guesthouse-demo", "A guesthouse", "/guesthouse-demo")]
    [TestCase("/demos/restaurant-demo", "A restaurant", "/restaurant-demo")]
    [TestCase("/demos/law-practice-demo", "A law practice", "/law-practice-demo")]
    [TestCase("/demos/coding-agent", "The coding agent", "/coding-agent")]
    [TestCase("/demos/triage-demo", "The triage team", "/triage-demo")]
    public void OpenedAtADemosAddress_ThatDemoIsForward(string address, string name, string wayIn)
    {
        using var ctx = SitePageContext.Create();
        // The site's addresses, as Program.cs registers them from its pages' own routes.
        ctx.Services.AddSingleton(SiteAddresses.From("home", typeof(DemosPage).Assembly));
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(address);

        var cut = ctx.Render<DemosPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("#top .start-caption h2").TextContent, Is.EqualTo(name));
            Assert.That(cut.Find("#top a.start-play").GetAttribute("href"), Is.EqualTo(wayIn));
        });
    }
}
