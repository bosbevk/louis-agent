using Microsoft.AspNetCore.Components;
using LouisAgent.Site.Layout;
using LouisAgent.Site.Pages.Agents;

namespace LouisAgent.Site.Tests.Pages.Agents;

/// <summary>
/// The demo index (F122, Louis: "a button there called demos", "which takes you to the demo index"); since F127.1 the
/// showroom's start: a big start for the guesthouse (Louis: "make the guesthouse the main one"), then a cover per demo.
/// </summary>
public class DemosPageTests
{
    /// <summary>
    /// The page is at /demos, in the showroom, leads with the guesthouse, and has a cover for each demo with its
    /// link, the businesses first.
    /// </summary>
    [Test]
    public void ThePage_LeadsWithTheGuesthouse_AndCoversEachDemo()
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
            Assert.That(cut.Find("#top .start-preview img").GetAttribute("src"),
                Is.EqualTo("images/showroom/seemeeu-banner.webp"), "a look at the guesthouse");
            Assert.That(cut.FindAll("h1"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("a.start-cover").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[]
                {
                    "/guesthouse-demo", "/restaurant-demo", "/law-practice-demo", "/coding-agent", "/triage-demo"
                }));
            Assert.That(cut.Markup, Does.Not.Contain(";"), "no semicolons in the site's words");
        });
    }
}
