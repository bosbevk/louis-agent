using Microsoft.AspNetCore.Components;
using LouisAgent.Site.Layout;
using LouisAgent.Site.Pages.Agents;

namespace LouisAgent.Site.Tests.Pages.Agents;

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
            Assert.That(cut.FindAll("h1").Select(h => h.TextContent),
                Is.EqualTo(new[] { "Watch a team triage a real error." }));
            Assert.That(cut.FindAll(".triage-player"), Has.Count.EqualTo(1));
            Assert.That(cut.Markup, Does.Not.Contain(";"), "no semicolons in the site's words");
        });
    }

    /// <summary>
    /// The page is in the showroom (F127.1, step 8): its stage in the team's purple, the player across the whole stage,
    /// since it shows its own steps and needs no cues, and the offer under it.
    /// </summary>
    [Test]
    public void ThePage_IsAShowroomStage_ThePlayerAcrossIt()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<TriagePage>();

        Assert.Multiple(() =>
        {
            Assert.That(typeof(TriagePage).GetCustomAttributes(typeof(LayoutAttribute), false)
                .Cast<LayoutAttribute>().Single().LayoutType, Is.EqualTo(typeof(ShowroomLayout)));
            Assert.That(cut.Find(".demo-stage").ClassList, Does.Contain("accent-purple"));
            Assert.That(cut.Find("#the-demo .triage-player"), Is.Not.Null);
            Assert.That(cut.Find(".stage-body").ClassList, Does.Contain("is-alone"));
            Assert.That(cut.Find("#ask a").GetAttribute("href"), Is.EqualTo("/contact"));
        });
    }
}
