using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Theme.Glass.Tests;

/// <summary>
/// F151.2: a site wears the glass theme by naming it among its areas, and its areas then list glass, once, however
/// often it is named.
/// </summary>
public class GlassThemeTests
{
    /// <summary>The areas a site names.</summary>
    private static SiteAreas Named(Action<SiteAreasBuilder> name) =>
        new ServiceCollection().AddSiteAreas(name).BuildServiceProvider().GetRequiredService<SiteAreas>();

    /// <summary>Naming glass lists one theme, glass, by its id and its name.</summary>
    [Test]
    public void NamingGlass_ListsTheGlassTheme()
    {
        var themes = Named(areas => areas.Glass()).Themes;

        Assert.That(themes, Is.EqualTo(new[] { new SiteTheme("glass", "Glass") }));
    }

    /// <summary>Naming glass twice still lists it once, as any area named twice counts once.</summary>
    [Test]
    public void NamingGlassTwice_ListsItOnce() =>
        Assert.That(Named(areas => areas.Glass().Glass()).Themes, Has.Count.EqualTo(1));
}
