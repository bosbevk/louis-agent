using System.Reflection;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Site.Pages;
using LouisAgent.Site.Pages.Agents;
using LouisAgent.SiteApi;
using LouisAgent.SiteApi.Resumes;
using LouisAgent.Web.Components.Site.Showroom;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Host;

/// <summary>
/// FX155: a search engine runs no script, so it reads the page the host renders itself. That page is drawn with the
/// services the host hands it, and the host's list lacked the site's demos. So the showroom's start reached a crawler
/// with no demo in it. These tests draw each page with the host's own list, the one the program uses.
/// </summary>
/// <remarks>
/// What is read here is what the host serves: a page drawn once, at its own route, and without its layout. So a
/// frame's menu or rail is never part of it, and every place on a page gets that page's one HTML.
/// </remarks>
public class PagesForACrawlerTests
{
    /// <summary>Every page of the site, by its address.</summary>
    private static IEnumerable<TestCaseData> Pages() =>
        typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>()
                .Where(route => !route.Template.Contains('{'))
                .Select(route => new TestCaseData(type, route.Template).SetArgDisplayNames(route.Template)));

    /// <summary>
    /// The page at its own route as the host gives it to a crawler, or nothing when it couldn't be drawn.
    /// </summary>
    private static async Task<string?> BodyAt(Type page, string route)
    {
        // No folder: the CV is the built-in one, as on a host where none was saved.
        var prerenderer = new PagePrerenderer(services => SitePageServices.Register(services, new ResumeStore(null)));
        var rendered = await prerenderer.RenderAsync(page, new Uri(new Uri("https://louis-agent.dev"), route));
        return rendered?.Body;
    }

    /// <summary>
    /// The prerenderer gives nothing when a page throws, and the host then serves the plain shell. So every page must
    /// still draw with the host's list, or a crawler silently loses it.
    /// </summary>
    [TestCaseSource(nameof(Pages))]
    public async Task EveryPage_IsDrawn(Type page, string route) =>
        Assert.That(await BodyAt(page, route), Is.Not.Null, "the page renders with the host's services");

    /// <summary>
    /// The showroom's start reaches a crawler with its demos: each business named, with its window's look at its
    /// own website, the featured one forward, and the way in leading to it.
    /// </summary>
    [Test]
    public async Task TheStart_HasItsDemos()
    {
        var onTheStart = SiteDemos.All.All.Where(demo => demo.Group == DemoGroup.ForYourBusiness).ToList();

        string? body = await BodyAt(typeof(DemosPage), "/demos");

        Assert.Multiple(() =>
        {
            Assert.That(onTheStart.Where(demo => body?.Contains(demo.Name) != true).Select(demo => demo.Name),
                Is.Empty, "a demo the crawler's page doesn't name");
            Assert.That(onTheStart.Where(demo => body?.Contains(demo.WindowAddress ?? "no window") != true)
                .Select(demo => demo.Name), Is.Empty, "a demo whose window the crawler's page doesn't have");
            Assert.That(body, Does.Match("<h2 class=\"start-name\"[^>]*>A guesthouse</h2>"));
            Assert.That(body, Does.Match("<a class=\"start-play\"[^>]*href=\"/guesthouse-demo\""));
        });
    }
}
