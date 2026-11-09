using System.Reflection;
using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Layout;

/// <summary>
/// A section's own address opens from outside the app too (FX124): a link, a reload or a search result at
/// <c>/home/for-you</c> showed "Not found", because the site's router looks only in the site's assembly and the
/// framework's place page lives in the framework's. The site carries the catch-all itself, and no admin page with it.
/// </summary>
public class SiteRouterTests
{
    /// <summary>The routes the site's router knows: the site's own assembly, which is all it is given.</summary>
    private static List<(Type Page, string Template)> Routes() =>
        typeof(App).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>(inherit: false)
                .Select(route => (type, route.Template)))
            .ToList();

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

    /// <summary>The site routes none of the framework's admin pages: a visitor's site has no admin.</summary>
    [Test]
    public void TheSite_RoutesNoAdminPage() =>
        Assert.That(Routes().Where(route => route.Template.StartsWith("/admin")), Is.Empty);
}
