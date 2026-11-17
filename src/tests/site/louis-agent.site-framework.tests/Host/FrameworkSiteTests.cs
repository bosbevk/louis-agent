using LouisAgent.Api.Site.Rendering;
using LouisAgent.SiteFramework;
using LouisAgent.SiteFrameworkApi;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Framework.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.SiteFramework.Tests.Host;

/// <summary>
/// The framework's own site (F160.1) answers at the root of its address, framework.louis-agent.dev, with the page a
/// search engine reads drawn by its host, and offers the visitor the framework's two themes.
/// </summary>
public class FrameworkSiteTests
{
    /// <summary>The Overview at the root, drawn with the services the host hands a page, as a crawler gets it.</summary>
    [Test]
    public async Task TheOverview_IsAtTheRoot_DrawnForACrawler()
    {
        var prerenderer = new PagePrerenderer(FrameworkPageServices.Register);

        var rendered = await prerenderer.RenderAsync(typeof(OverviewPage), new Uri("https://framework.louis-agent.dev/"));

        Assert.Multiple(() =>
        {
            Assert.That(rendered, Is.Not.Null, "the page renders with the host's services");
            Assert.That(rendered!.Title, Does.Contain("louis-agent"));
            Assert.That(rendered.Body, Does.Contain("<h1"));
        });
    }

    /// <summary>
    /// The site opens in flat, light (Louis: "this ones default will be the flat white theme"), offers glass beside
    /// it, and turns on the framework's pages, a package as the accounts are.
    /// </summary>
    [Test]
    public void TheSite_OpensInFlat_ThenOffersGlass()
    {
        var areas = new ServiceCollection().AddSiteAreas(FrameworkAreas.Name).BuildServiceProvider()
            .GetRequiredService<SiteAreas>();
        string page = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/louis-agent.site-framework/wwwroot/index.html"));

        Assert.Multiple(() =>
        {
            Assert.That(areas.Themes.Select(theme => theme.Id), Is.EqualTo(new[] { "flat", "glass" }));
            Assert.That(areas.Names, Does.Contain("framework"));
            Assert.That(page, Does.Contain("<html lang=\"en\" data-theme=\"flat\" data-style=\"light\">"),
                "the page's first paint is flat, light");
        });
    }
}
