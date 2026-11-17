using LouisAgent.Web.Components.Areas;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests;

/// <summary>
/// F160.1: the framework's pages are a package a site turns on with <c>.Framework()</c>, as it turns on the accounts
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
}
