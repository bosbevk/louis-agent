using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Screens;
using LouisAgent.Web.Components.Screens.Table;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Areas;

/// <summary>
/// F144: the packs' screens and uploads are an area of their own inside the admin's frame, named with
/// <c>.Packs(views)</c>: the views a site sets for its packs, the menu in the sidebar and the card on the home.
/// </summary>
public class PacksAreaTests
{
    /// <summary>A layout the admin is named with.</summary>
    private sealed class Layout : LayoutComponentBase;

    /// <summary>What the site's areas register.</summary>
    private static IServiceProvider Named(Action<SiteAreasBuilder> name) =>
        new ServiceCollection().AddSiteAreas(name).BuildServiceProvider();

    /// <summary>The packs plug into the admin with the site's views, the menu and the card.</summary>
    [Test]
    public void ThePacks_AreAnAdminArea_WithTheSitesViewsTheMenuAndTheCard()
    {
        var services = Named(areas => areas.Admin(typeof(Layout)).Packs(views => views.UseScreen<ScreenTable>("own")));

        var areas = services.GetRequiredService<SiteAreas>();
        Assert.Multiple(() =>
        {
            Assert.That(areas.Names, Is.EqualTo(new[] { "admin", "packs" }));
            Assert.That(areas.AdminLinks, Is.EqualTo(new[] { typeof(PackMenu) }));
            Assert.That(areas.AdminCards, Is.EqualTo(new[] { typeof(PacksCard) }));
            Assert.That(services.GetRequiredService<PackViews>().ScreenView("own"), Is.EqualTo(typeof(ScreenTable)));
        });
    }

    /// <summary>The admin alone has no packs: no views, no menu, no card.</summary>
    [Test]
    public void TheAdminAlone_HasNoPacks()
    {
        var services = Named(areas => areas.Admin(typeof(Layout)));

        Assert.Multiple(() =>
        {
            Assert.That(services.GetService<PackViews>(), Is.Null);
            Assert.That(services.GetRequiredService<SiteAreas>().AdminLinks, Is.Empty);
        });
    }
}
