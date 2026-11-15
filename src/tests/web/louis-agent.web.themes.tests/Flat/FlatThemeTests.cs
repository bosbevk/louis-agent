using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Themes.Flat;
using LouisAgent.Web.Themes.Glass;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Themes.Tests.Flat;

/// <summary>
/// F151.2: a site wears the flat theme by naming it among its areas, and its areas then list flat, once, however
/// often it is named.
/// </summary>
public class FlatThemeTests
{
    /// <summary>The areas a site names.</summary>
    private static SiteAreas Named(Action<SiteAreasBuilder> name) =>
        new ServiceCollection().AddSiteAreas(name).BuildServiceProvider().GetRequiredService<SiteAreas>();

    /// <summary>Naming flat lists one theme, flat, by its id and its name.</summary>
    [Test]
    public void NamingFlat_ListsTheFlatTheme()
    {
        var themes = Named(areas => areas.Flat()).Themes;

        Assert.That(themes, Is.EqualTo(new[] { new SiteTheme("flat", "Flat") }));
    }

    /// <summary>Naming flat twice still lists it once, as any area named twice counts once.</summary>
    [Test]
    public void NamingFlatTwice_ListsItOnce() =>
        Assert.That(Named(areas => areas.Flat().Flat()).Themes, Has.Count.EqualTo(1));

    /// <summary>A site that names glass and then flat lists both, glass first, so glass is its default.</summary>
    [Test]
    public void GlassThenFlat_ListsBoth_GlassFirst() =>
        Assert.That(Named(areas => areas.Glass().Flat()).Themes.Select(theme => theme.Id),
            Is.EqualTo(new[] { "glass", "flat" }));
}
