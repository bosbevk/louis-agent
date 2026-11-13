using System.Reflection;
using LouisAgent.Web.Components.Admin.Overview;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Overview;
using LouisAgent.Web.Pages.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Admin;

/// <summary>
/// F127.8, Louis: louis-agent.dev's admin, behind the admin key for now; his pick of three designs, the overview first.
/// The site names its admin through its areas (ADR 0120), counting the guide's leads, and the framework's admin pages,
/// the overview among them, are its router's.
/// </summary>
public class SiteAdminTests
{
    /// <summary>The site's areas, as its program names them.</summary>
    private static ServiceProvider Areas() =>
        new ServiceCollection().AddSiteAreas(LouisAgentDevAreas.Name).BuildServiceProvider();

    /// <summary>The overview counts the guide's leads, by the tool that sends one.</summary>
    [Test]
    public void TheAdmin_CountsTheGuidesLeads() =>
        Assert.That(Areas().GetRequiredService<OverviewCounts>().Tools,
            Is.EqualTo(new[] { new CountedTool("SendLead", "leads") }));

    /// <summary>
    /// F144: the site's admin plugs in the overview alone, its packs offer no screen, and it runs no evals, so its home
    /// draws the overview's card and nothing else.
    /// </summary>
    [Test]
    public void TheAdminsHome_IsTheOverviewsCardAlone()
    {
        var areas = Areas().GetRequiredService<SiteAreas>();

        Assert.Multiple(() =>
        {
            Assert.That(areas.AdminCards, Is.EqualTo(new[] { typeof(OverviewCard) }));
            Assert.That(areas.PageAssemblies.SelectMany(page => page.GetTypes())
                    .SelectMany(type => type.GetCustomAttributes<RouteAttribute>()).Select(route => route.Template),
                Does.Contain("/admin"), "the home opens");
        });
    }

    /// <summary>The router has the framework's admin pages, so /admin/overview opens.</summary>
    [Test]
    public void TheRouter_HasTheOverview()
    {
        var pages = Areas().GetRequiredService<SiteAreas>().PageAssemblies;

        Assert.That(pages.SelectMany(page => page.GetTypes())
                .SelectMany(type => type.GetCustomAttributes<RouteAttribute>()).Select(route => route.Template),
            Does.Contain("/admin/overview"));
    }

    /// <summary>
    /// The site's router takes the pages its areas bring, as every site's does (F127.6), so /admin/overview opens
    /// rather than the site's not-found page (Louis, opening it on the PC).
    /// </summary>
    [Test]
    public void TheRouter_TakesTheAreasPages() =>
        Assert.That(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site/App.razor")),
            Does.Contain("AdditionalAssemblies=\"Areas.PageAssemblies\""));

    /// <summary>
    /// No route is the site's and an area's both: the router refuses two pages on one route, and the whole site fails
    /// (Louis's /admin/overview: the site's own catch-all and the framework's, once the admin brought its pages).
    /// </summary>
    [Test]
    public void NoRoute_IsTwice()
    {
        var assemblies = Areas().GetRequiredService<SiteAreas>().PageAssemblies.Append(typeof(App).Assembly).Distinct();
        var routes = assemblies.SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>().Select(route => route.Template));

        Assert.That(routes.GroupBy(route => route).Where(same => same.Count() > 1).Select(same => same.Key),
            Is.Empty);
    }

    /// <summary>The site's secrets name the admin's own key, which falls back to the API's.</summary>
    [Test]
    public void TheSecrets_NameTheAdminsKey() =>
        Assert.That(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/site/config/.env.secrets.example")),
            Does.Contain("ADMIN_API_KEY="));

    /// <summary>The overview page is the framework's, never the site's own copy.</summary>
    [Test]
    public void TheOverview_IsTheFrameworks() =>
        Assert.That(typeof(Overview).Assembly.GetName().Name, Is.EqualTo("louis-agent.web"));
}
