using LouisAgent.CodeReview.Pages.Site.Agents;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Tests.Pages.Site.Agents;

/// <summary>
/// The triage demo's page (F106.3.4): a visitor watches the triage team handle one real error of the made-up order
/// service, recorded once, step by step, then gets the offer. The Demos menu and the demo index lead here (F122).
/// </summary>
public class TriagePageTests
{
    /// <summary>
    /// The page is at /triage-demo (F122, Louis: "should just say triage-demo"), opens with its heading, and plays the
    /// pack's recorded run.
    /// </summary>
    [Test]
    public void ThePage_PlaysTheRecordedRun()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<TriagePage>();

        var route = typeof(TriagePage).GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>()
            .Single().Template;
        Assert.Multiple(() =>
        {
            Assert.That(route, Is.EqualTo("/triage-demo"));
            Assert.That(cut.Find("h2.chrome").TextContent, Is.EqualTo("Watch a team triage a real error."));
            Assert.That(cut.FindAll(".triage-player"), Has.Count.EqualTo(1));
            Assert.That(cut.Markup, Does.Not.Contain(";"), "no semicolons in the site's words");
        });
    }
}
