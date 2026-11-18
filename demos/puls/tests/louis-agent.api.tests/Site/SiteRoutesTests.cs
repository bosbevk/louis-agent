using LouisAgent.Api.Contracts.Site;
using LouisAgent.Api.Site;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Site;

/// <summary>
/// Which pages the host draws for a search engine. F142: a site may keep an admin page of its own in its project
/// (louis-agent.dev's CV editor); it sits behind the admin key, so it is no public page. F162.1: the pages an area
/// brings, such as the framework's or the accounts', are the site's pages too, but a page behind a sign-in never is
/// (Louis: the sign-in pages are public, "not the pages after the sign in").
/// </summary>
public class SiteRoutesTests
{
    /// <summary>An admin page a site keeps in its own project.</summary>
    [Route("/admin/things")]
    private sealed class AdminThings : ComponentBase;

    /// <summary>A page only a signed-in person sees, as an account's profile is.</summary>
    [Route("/mine")]
    [Authorize]
    private sealed class MyThings : ComponentBase;

    /// <summary>A public page an area brings, as a package's sign-in page is.</summary>
    [Route("/come-in")]
    private sealed class ComeIn : ComponentBase;

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

    /// <summary>A page behind a sign-in names no page the host draws, and is in none of its lists.</summary>
    [Test]
    public void APageBehindASignIn_IsNoPublicPage()
    {
        var routes = SiteRoutes.Of(typeof(SiteRoutesTests).Assembly, home: null);

        Assert.Multiple(() =>
        {
            Assert.That(routes.PageFor("/mine"), Is.Null);
            Assert.That(routes.Pages.Select(page => page.Page), Has.None.EqualTo(typeof(MyThings)));
        });
    }

    /// <summary>
    /// The pages the site's areas bring, told through the site's services, are the site's pages as its own are: a
    /// site with none of its own still draws them.
    /// </summary>
    [Test]
    public void AnAreasPages_AreTheSitesPages()
    {
        var areaPages = new SitePageAssemblies([typeof(SiteRoutesTests).Assembly]);

        var routes = SiteRoutes.Of(typeof(string).Assembly, services => services.AddSingleton(areaPages), home: null);

        Assert.That(routes.PageFor("/come-in")?.Page, Is.EqualTo(typeof(ComeIn)));
    }
}
