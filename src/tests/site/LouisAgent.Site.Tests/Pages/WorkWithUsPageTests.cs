using LouisAgent.Site.Pages;
using LouisAgent.Web.Components.Site.Slides;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// The Work with us page (F177): the slideshow a client is handed, as a page of the site. It is laid out as slides,
/// one screen a section, and tells the work in the order a client asks about it.
/// </summary>
public class WorkWithUsPageTests
{
    /// <summary>The page asks the frame for slides, and its nine sections stand in the deck's order.</summary>
    [Test]
    public void ThePage_IsNineSlides_InTheDecksOrder()
    {
        using var ctx = SitePageContext.Create();

        var page = ctx.Render<WorkWithUsPage>();

        Assert.Multiple(() =>
        {
            Assert.That(page.FindComponent<PageLayoutChoice>().Instance.Layout, Is.EqualTo(PageLayout.Slides));
            Assert.That(string.Join(' ', page.FindAll("section").Select(section => section.Id)), Is.EqualTo(
                "top what-we-build who see-it-working how-we-work how-you-pay what-you-get running-costs next-step"));
        });
    }

    /// <summary>The work is shown as seven numbered steps, and the price as two halves.</summary>
    [Test]
    public void TheWork_IsSevenSteps_AndThePriceTwoHalves()
    {
        using var ctx = SitePageContext.Create();

        var page = ctx.Render<WorkWithUsPage>();

        Assert.Multiple(() =>
        {
            Assert.That(page.FindAll("#how-we-work .steps li"), Has.Count.EqualTo(7));
            Assert.That(page.FindAll("#how-you-pay .stat-value").Select(value => value.TextContent.Trim()),
                Is.EqualTo(new[] { "50%", "50%" }));
        });
    }

    /// <summary>What we built can be tried: the demos on this site, and the framework's own site in a new tab.</summary>
    [Test]
    public void SeeItWorking_LeadsToTheDemos_AndTheFrameworksSite()
    {
        using var ctx = SitePageContext.Create();

        var page = ctx.Render<WorkWithUsPage>();

        Assert.Multiple(() =>
        {
            Assert.That(page.FindAll("#see-it-working a[href='/demos']"), Is.Not.Empty);
            Assert.That(page.Find("#see-it-working a[href='https://framework.louis-agent.dev']")
                .GetAttribute("target"), Is.EqualTo("_blank"));
        });
    }

    /// <summary>The page ends on one next step: the visitor's details with the guide, or the contact page.</summary>
    [Test]
    public void TheLastSlide_AsksForTheNextStep()
    {
        using var ctx = SitePageContext.Create();

        var page = ctx.Render<WorkWithUsPage>();

        Assert.That(page.FindAll("#next-step a[href='/contact']"), Is.Not.Empty);
    }
}
