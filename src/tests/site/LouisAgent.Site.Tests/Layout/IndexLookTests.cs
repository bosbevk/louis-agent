using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Testing.Sites;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Layout;

/// <summary>
/// F151.1: louis-agent.dev's page opens in the look its areas give, the default theme they name (or none) and the
/// default style, written on its &lt;html&gt; before any script runs.
/// </summary>
public class IndexLookTests
{
    /// <summary>What the page writes is what the site's areas say.</summary>
    [Test]
    public void ThePage_WritesTheLookTheAreasGive() =>
        Assert.That(IndexLook.Of(SiteProject.Folder("LouisAgent.Site")),
            Is.EqualTo(IndexLook.For(LouisAgentDevAreas.Name)));

    /// <summary>
    /// The site offers the framework's two themes, glass first, so glass is what a first visit gets (F151.6).
    /// </summary>
    [Test]
    public void TheSite_OffersGlassThenFlat()
    {
        var areas = new ServiceCollection().AddSiteAreas(LouisAgentDevAreas.Name).BuildServiceProvider()
            .GetRequiredService<SiteAreas>();

        Assert.That(areas.Themes.Select(theme => theme.Id), Is.EqualTo(new[] { "glass", "flat" }));
    }
}
