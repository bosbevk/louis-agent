using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Evals.Admin;
using LouisAgent.Web.Evals.Client;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests;

/// <summary>
/// The eval pages are an area a site names with <c>.Evals()</c>: their pages, the client they read with, the
/// sidebar's link and the home's card. A site that names no evals ships none of it.
/// </summary>
public class EvalsAreaTests
{
    /// <summary>The admin's layout a site gives its admin pages.</summary>
    private sealed class AdminLayout;

    /// <summary>Named beside the admin, the evals register their pages, client, link and card.</summary>
    [Test]
    public void TheEvals_AreAnArea_WithTheirPagesClientLinkAndCard()
    {
        var services = new ServiceCollection();

        services.AddSiteAreas(areas => areas.Admin(typeof(AdminLayout)).Evals());

        var areas = (SiteAreas)services.Single(d => d.ServiceType == typeof(SiteAreas)).ImplementationInstance!;
        Assert.Multiple(() =>
        {
            Assert.That(areas.Names, Does.Contain("evals"));
            Assert.That(areas.PageAssemblies, Does.Contain(EvalsLibrary.Assembly));
            Assert.That(areas.AdminLinks, Does.Contain(typeof(EvalsLinks)));
            Assert.That(areas.AdminCards, Does.Contain(typeof(EvalsCard)));
            Assert.That(services.Any(d => d.ServiceType == typeof(EvalsApi)), Is.True);
        });
    }
}
