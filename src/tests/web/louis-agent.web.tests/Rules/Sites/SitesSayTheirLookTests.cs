using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Sites;

namespace LouisAgent.Web.Tests.Rules.Sites;

/// <summary>
/// F151.1: every site's page opens in the default style, written on its <c>&lt;html&gt;</c> before any script runs,
/// so a first visit is light whatever the device says. The theme it writes is each site's own test's to hold, since
/// only a site's tests can call its areas (Louis's pick A of three).
/// </summary>
public class SitesSayTheirLookTests
{
    /// <summary>The sites whose page is a static <c>index.html</c>.</summary>
    private static IEnumerable<string> Sites() =>
        FrameworkWebSites.Folders().Where(site => File.Exists(Path.Combine(site, "wwwroot", "index.html")));

    /// <summary>Every site's page writes the default style.</summary>
    [Test]
    public void EverySitesPage_WritesTheDefaultStyle()
    {
        var sites = Sites().ToList();
        Assert.That(sites, Has.Count.GreaterThanOrEqualTo(4), "the sites were found");

        var astray = sites
            .Where(site => IndexLook.Of(site).Style != SiteLook.DefaultStyle.Wire())
            .Select(FrameworkWebSites.NameOf);

        Assert.That(astray, Is.Empty);
    }
}
