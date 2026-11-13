using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Evals;
using LouisAgent.Web.Evals.Admin.Explorer.Sites;
using LouisAgent.Web.Evals.Components.Head;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests;

/// <summary>
/// F127.6: the eval studio is an area a site names with <c>.Evals()</c>: its services, its pages for the router, and
/// its links, which the admin frame draws under the pack menu.
/// </summary>
public class EvalAreaTests
{
    /// <summary>A layout for the admin's frame, which an admin area plugs into (F144).</summary>
    private sealed class AdminLayout : Microsoft.AspNetCore.Components.LayoutComponentBase;

    /// <summary>Named inside the admin's frame, the eval studio brings its services, pages and admin links.</summary>
    [Test]
    public void TheEvalStudio_IsAnArea_WithItsServicesPagesAndLinks()
    {
        var services = new ServiceCollection();

        services.AddSiteAreas(areas => areas.Admin(typeof(AdminLayout)).Evals());

        var areas = (SiteAreas)services.Single(d => d.ServiceType == typeof(SiteAreas)).ImplementationInstance!;
        Assert.Multiple(() =>
        {
            Assert.That(areas.Names, Is.EqualTo(new[] { "admin", "evals" }));
            Assert.That(areas.PageAssemblies, Does.Contain(EvalLibrary.Assembly));
            Assert.That(areas.AdminLinks, Is.EqualTo(new[] { typeof(EvalLinks) }));
            Assert.That(services.Any(d => d.ServiceType == typeof(SiteKinds)), Is.True);
        });
    }
}
