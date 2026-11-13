using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using LouisAgent.Site.Content;
using LouisAgent.Site.Pages.About;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Sources;
using LouisAgent.Web.Components.Site.Slides;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Pages.About;

/// <summary>
/// F142: Louis's CV is a page of the site at <c>/cv</c>: his name, the ways to reach him and his photo, then the CV's
/// sections from its record, on one scrolling page (his approved page), with the page's own closing line. It shows
/// the copy the API holds, so an edit saved in the admin shows, and the built-in one until that answers.
/// </summary>
public class CvPageTests
{
    /// <summary>The page; it draws the built-in CV at once, and the source's copy when that answers.</summary>
    private static IRenderedComponent<CvPage> Render(BunitContext ctx) => ctx.Render<CvPage>();

    /// <summary>Waits until the hero's summary says this.</summary>
    private static void WaitForSummary(IRenderedComponent<CvPage> page, string summary) =>
        page.WaitForAssertion(() => Assert.That(page.Find(".cv-hero .lede").TextContent, Is.EqualTo(summary)),
            TimeSpan.FromSeconds(10));

    /// <summary>The page is at /cv, headed by Louis's name, with the links a recruiter needs.</summary>
    [Test]
    public void TheCv_IsAtCv_WithLouissNameAndLinks()
    {
        using var ctx = SitePageContext.Create();

        var page = Render(ctx);

        var route = typeof(CvPage).GetCustomAttribute<RouteAttribute>()!.Template;
        Assert.Multiple(() =>
        {
            Assert.That(route, Is.EqualTo("/cv"));
            Assert.That(page.Find("h1.chrome").TextContent, Is.EqualTo("Louis Marais"));
            Assert.That(page.FindAll("a[href='https://www.linkedin.com/in/louismarais86']"), Is.Not.Empty);
            Assert.That(page.FindAll("a[href='https://github.com/bosbevk']"), Is.Not.Empty);
            Assert.That(page.Find("a.btn-whatsapp").TextContent, Does.Contain("066 391 2470"),
                "the WhatsApp button says his number");
            Assert.That(page.Find("img.cv-photo").GetAttribute("src"), Is.EqualTo("images/louis-marais.jpg"));
            Assert.That(page.Find(".cv-where").TextContent, Does.Contain("open to remote and hybrid"));
            Assert.That(page.Find(".cv-end").TextContent, Does.Contain("References on request"));
            Assert.That(page.Find("button.cv-print").TextContent.Trim(), Is.EqualTo("Save as PDF"));
        });
    }

    /// <summary>The page scrolls as one column, each section an area with a name the guide can show.</summary>
    [Test]
    public void TheCv_IsOneScrollingPage_OfNamedAreas()
    {
        using var ctx = SitePageContext.Create();

        var page = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(page.FindComponent<PageLayoutChoice>().Instance.Layout, Is.EqualTo(PageLayout.Flow));
            Assert.That(string.Join(' ', page.FindAll("section.band").Select(band => band.Id)),
                Is.EqualTo("top now experience skills education also"));
        });
    }

    /// <summary>The copy the site's API holds is the one shown, so an edit saved in the admin shows.</summary>
    [Test]
    public void TheCv_IsTheOneTheApiHolds()
    {
        using var ctx = SitePageContext.Create();
        var saved = Cv.Resume with { Basics = Cv.Resume.Basics with { Summary = "Saved in the admin." } };
        var answer = JsonContent.Create(saved, options: ResumeReader.Options);
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = answer });
        ctx.Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://site.test/") });

        var page = Render(ctx);

        WaitForSummary(page, "Saved in the admin.");
    }

    /// <summary>On the server, the host's own source is asked in place of the API.</summary>
    [Test]
    public void OnTheServer_TheHostsSourceIsAsked()
    {
        using var ctx = SitePageContext.Create();
        var stored = Cv.Resume with { Basics = Cv.Resume.Basics with { Summary = "From the store." } };
        ctx.Services.AddSingleton<IResumeSource>(new FixedSource(stored));

        var page = Render(ctx);

        WaitForSummary(page, "From the store.");
    }

    /// <summary>A source that answers one CV.</summary>
    private sealed class FixedSource(Resume resume) : IResumeSource
    {
        /// <summary>The CV it was given.</summary>
        public Task<Resume> ReadAsync() => Task.FromResult(resume);
    }
}
