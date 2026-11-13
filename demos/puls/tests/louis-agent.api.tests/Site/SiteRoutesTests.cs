using LouisAgent.Api.Site;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Api.Tests.Site;

/// <summary>
/// F142: a site may keep an admin page of its own in its project (louis-agent.dev's CV editor). It sits behind the
/// admin key, so it is no public page: never rendered for a search engine, never a place the guide can send a visitor.
/// </summary>
public class SiteRoutesTests
{
    /// <summary>An admin page a site keeps in its own project.</summary>
    [Route("/admin/things")]
    private sealed class AdminThings : ComponentBase;

    /// <summary>An address under /admin names no page of the site, and is in none of its lists.</summary>
    [Test]
    public void AnAdminPage_IsNoPublicPage()
    {
        var routes = SiteRoutes.Of(typeof(SiteRoutesTests).Assembly, home: null);

        Assert.Multiple(() =>
        {
            Assert.That(routes.PageFor("/admin/things"), Is.Null);
            Assert.That(routes.Pages.Select(page => page.Page), Has.None.EqualTo(typeof(AdminThings)));
        });
    }
}
