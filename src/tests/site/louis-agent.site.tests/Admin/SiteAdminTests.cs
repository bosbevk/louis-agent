using System.Reflection;
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

    /// <summary>The router has the framework's admin pages, so /admin/overview opens.</summary>
    [Test]
    public void TheRouter_HasTheOverview()
    {
        var pages = Areas().GetRequiredService<SiteAreas>().PageAssemblies;

        Assert.That(pages.SelectMany(page => page.GetTypes())
                .SelectMany(type => type.GetCustomAttributes<RouteAttribute>()).Select(route => route.Template),
            Does.Contain("/admin/overview"));
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
