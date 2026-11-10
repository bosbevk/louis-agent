using Microsoft.AspNetCore.Components;
using LouisAgent.Site.Pages.Agents;

namespace LouisAgent.Site.Tests.Pages.Agents;

/// <summary>
/// The demo index (F122, Louis: "a button there called demos", "which takes you to the demo index"): one tile per demo,
/// each saying what it shows and leading to it.
/// </summary>
public class DemosPageTests
{
    /// <summary>
    /// The page is at /demos, and lists the showroom's three businesses first (F128.2), then the coding agent's demo
    /// and the triage team's, each with its link.
    /// </summary>
    [Test]
    public void ThePage_ListsEachDemo_WithItsLink()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<DemosPage>();

        var route = typeof(DemosPage).GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>()
            .Single().Template;
        var links = cut.FindAll("article.panel a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(route, Is.EqualTo("/demos"));
            Assert.That(cut.FindAll("article.panel"), Has.Count.EqualTo(5));
            Assert.That(links,
                Is.EqualTo(new[]
                {
                    "/restaurant-demo", "/law-practice-demo", "/guesthouse-demo", "/coding-agent", "/triage-demo"
                }));
            Assert.That(cut.Markup, Does.Not.Contain(";"), "no semicolons in the site's words");
        });
    }
}
