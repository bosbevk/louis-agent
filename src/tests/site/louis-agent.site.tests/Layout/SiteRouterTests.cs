using System.Reflection;
using LouisAgent.Site.Admin;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Layout;

/// <summary>
/// A section's own address opens from outside the app too (FX124): a link, a reload or a search result at
/// <c>/home/for-you</c> showed "Not found" when the router knew no place page. Since F127.8 the site's router takes
/// the pages its areas bring, its admin among them, so the framework's place page comes with them and the site keeps
/// no copy of its own.
/// </summary>
public class SiteRouterTests
{
    /// <summary>The routes the site's router knows: the site's own pages, and those its areas bring.</summary>
    private static List<(Type Page, string Template)> Routes()
    {
        var areas = new ServiceCollection().AddSiteAreas(LouisAgentDevAreas.Name).BuildServiceProvider()
            .GetRequiredService<SiteAreas>();
        return areas.PageAssemblies.Append(typeof(App).Assembly).Distinct().SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>(inherit: false)
                .Select(route => (type, route.Template)))
            .ToList();
    }

    /// <summary>A place's address reaches the framework's place page, which draws the page it names.</summary>
    [Test]
    public void APlacesAddress_HasARoute_ToThePlacePage()
    {
        var catchAll = Routes().Where(route => route.Template == "/{*Path}").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(catchAll, Has.Count.EqualTo(1), "one catch-all for every place's address");
            Assert.That(catchAll.Select(route => route.Page), Has.All.Matches<Type>(page =>
                typeof(SectionPage).IsAssignableFrom(page)));
        });
    }

    /// <summary>
    /// The admin's pages are the framework's, behind the admin key (F127.8). The site writes one of its own, for the
    /// one area that is only this site's: the editor for Louis's CV (F142, "this belongs to .site only").
    /// </summary>
    [Test]
    public void TheAdminsPages_AreTheFrameworks_ButForTheCvsEditor() =>
        Assert.That(Routes().Where(route => route.Template.StartsWith("/admin"))
                .Where(route => route.Page.Assembly == typeof(App).Assembly).Select(route => route.Page),
            Is.EqualTo(new[] { typeof(ResumeEditor) }));
}
