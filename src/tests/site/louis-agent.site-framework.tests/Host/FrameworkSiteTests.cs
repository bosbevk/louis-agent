using LouisAgent.Api.Site.Rendering;
using LouisAgent.SiteFramework;
using LouisAgent.SiteFrameworkApi;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Docs;
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

    /// <summary>The site's doc links lead to the repo on GitHub, until the docs have a site of their own.</summary>
    [Test]
    public void TheSitesDocLinks_LeadToGitHub()
    {
        var docs = new ServiceCollection().AddSiteAreas(FrameworkAreas.Name).BuildServiceProvider()
            .GetService<DocsAddress>();

        Assert.That(docs?.For("src/docs/README.md"),
            Is.EqualTo("https://github.com/bosbevk/louis-agent/blob/main/src/docs/README.md"));
    }

    /// <summary>
    /// The site opens in flat, light, and offers flat alone, so a visitor picks only light or dark (Louis: "this ones
    /// default will be the flat white theme", then "this must always load flat so no glass theme here yet"), and
    /// turns on the framework's pages, a package as the accounts are.
    /// </summary>
    [Test]
    public void TheSite_OpensInFlat_AndOffersFlatAlone()
    {
        var areas = new ServiceCollection().AddSiteAreas(FrameworkAreas.Name).BuildServiceProvider()
            .GetRequiredService<SiteAreas>();
        string page = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/louis-agent.site-framework/wwwroot/index.html"));

        Assert.Multiple(() =>
        {
            Assert.That(areas.Themes.Select(theme => theme.Id), Is.EqualTo(new[] { "flat" }));
            Assert.That(areas.Names, Does.Contain("framework"));
            Assert.That(page, Does.Contain("<html lang=\"en\" data-theme=\"flat\" data-style=\"light\">"),
                "the page's first paint is flat, light");
        });
    }
}
