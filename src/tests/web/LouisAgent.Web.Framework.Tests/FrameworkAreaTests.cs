using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Docs.Contents;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests;

/// <summary>
/// F164.1: the framework's pages are a package a site turns on with <c>.Framework()</c>, as it turns on the accounts
/// (Louis: "the framework must be a package like accounts"): one area, which brings its pages to the site's router.
/// </summary>
public class FrameworkAreaTests
{
    /// <summary>Named, the framework is one area, and its pages join the site's router.</summary>
    [Test]
    public void TheFramework_IsAnArea_WithItsPages()
    {
        var services = new ServiceCollection();

        services.AddSiteAreas(areas => areas.Framework());

        var areas = (SiteAreas)services.Single(d => d.ServiceType == typeof(SiteAreas)).ImplementationInstance!;
        Assert.Multiple(() =>
        {
            Assert.That(areas.Names, Is.EqualTo(new[] { "framework" }));
            Assert.That(areas.PageAssemblies, Does.Contain(FrameworkLibrary.Assembly));
        });
    }

    /// <summary>
    /// The framework's pages bring their own contents, in reading order, for the docs layout's menu and page turns:
    /// every page in it is a page of the package, so the menu never leads nowhere.
    /// </summary>
    [Test]
    public void TheFramework_BringsItsContents_EachAPageOfIt()
    {
        var services = new ServiceCollection();
        services.AddSiteAreas(areas => areas.Framework());

        var contents = services.BuildServiceProvider().GetRequiredService<DocsContents>();
        var routes = FrameworkLibrary.Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>())
            .Select(route => route.Template).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(contents.Groups.SelectMany(group => group.Entries).Select(entry => entry.Address),
                Is.SubsetOf(routes));
            Assert.That(contents.FindEntry("/")?.Title, Is.EqualTo("Overview"));
        });
    }
}
