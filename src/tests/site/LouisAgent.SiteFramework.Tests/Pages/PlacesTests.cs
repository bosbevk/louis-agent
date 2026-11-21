using System.Reflection;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.SiteFramework.Tests.Pages;

/// <summary>
/// Every place on the framework's pages has its own address (ADR 0107): <c>/overview/the-engine</c> opens the
/// Overview at its engine, because the site names the places area, which brings the catch-all that draws a place's
/// page and reads the addresses from the pages the site and its areas serve.
/// </summary>
public class PlacesTests
{
    /// <summary>The route a place's address comes to, when no page's own route matches it.</summary>
    private const string CatchAll = "/{*Path}";

    /// <summary>The site's areas, as the browser's program and the host both name them.</summary>
    private static ServiceProvider Site()
    {
        var services = new ServiceCollection();
        services.AddSiteAreas(FrameworkAreas.Name);
        return services.BuildServiceProvider();
    }

    /// <summary>A family's address names the Overview, at that family's section.</summary>
    [Test]
    public void AFamilysAddress_NamesTheOverviewAtItsSection()
    {
        using var site = Site();

        var read = site.GetService<SiteAddresses>()?.Read("/overview/the-engine");

        Assert.That(read, Is.EqualTo(new SiteAddress("/", "the-engine")));
    }

    /// <summary>The site's router has the catch-all, so an address that is a place, not a page, is drawn.</summary>
    [Test]
    public void TheRouter_HasTheCatchAll()
    {
        using var site = Site();

        var routes = site.GetRequiredService<SiteAreas>().PageAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => route.Template);

        Assert.That(routes, Does.Contain(CatchAll));
    }
}
