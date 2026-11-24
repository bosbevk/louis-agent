using System.Reflection;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Evals.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.SiteFramework.Tests.Admin;

/// <summary>
/// F126.5.2, Louis: "framework needs an admin page for the evals". The framework site names the framework's admin
/// frame with a layout of its own, its brand in the framework's <c>AdminShell</c> as louis-agent.dev's is, and the
/// evals' area in it (his pick, "Its own brand layout"; ADR 0125), behind the admin key.
/// </summary>
public class FrameworkAdminTests
{
    /// <summary>The site's areas, as its program names them.</summary>
    private static SiteAreas Areas() =>
        new ServiceCollection().AddSiteAreas(FrameworkAreas.Name).BuildServiceProvider()
            .GetRequiredService<SiteAreas>();

    /// <summary>The admin's home draws the evals' card and its sidebar the evals' link, and nothing else.</summary>
    [Test]
    public void TheAdmin_HoldsTheEvals_AndNothingElse()
    {
        var areas = Areas();

        Assert.Multiple(() =>
        {
            Assert.That(areas.AdminCards, Is.EqualTo(new[] { typeof(EvalsCard) }));
            Assert.That(areas.AdminLinks, Is.EqualTo(new[] { typeof(EvalsLinks) }));
        });
    }

    /// <summary>The router has the admin's home and the evals' page, so /admin opens and /admin/evals too.</summary>
    [Test]
    public void TheRouter_HasTheAdminsHome_AndTheEvalsPage()
    {
        var routes = Areas().PageAssemblies.SelectMany(page => page.GetTypes())
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>()).Select(route => route.Template);

        Assert.That(routes, Does.Contain("/admin").And.Contain("/admin/evals"));
    }

    /// <summary>The site's admin layout fills the framework's frame with this site's name and its way back.</summary>
    [Test]
    public void TheLayout_FillsTheFrameworksFrame_WithThisSitesName()
    {
        string layout = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/LouisAgent.SiteFramework/Layout/AdminLayout.razor"));

        Assert.That(layout, Does.Contain("<AdminShell BrandTitle=\"louis-agent framework\" BackHref=\"/\""));
    }
}
