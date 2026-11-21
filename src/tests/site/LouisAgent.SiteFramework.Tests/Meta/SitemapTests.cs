using System.Xml.Linq;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.SiteFramework.Tests.Meta;

/// <summary>
/// The framework's site lists every page by its own address (F164.1, FX216), in the menu's reading order, so a search
/// engine finds them all and only the originals; its robots.txt lets every crawler in and names the sitemap. The file
/// is held to the pages, and must say the same. <c>LOUIS_UPDATE_GOLDEN=1</c> writes it, as it writes the golden files.
/// </summary>
public class SitemapTests
{
    /// <summary>The site's address on the web.</summary>
    private const string Site = "https://framework.louis-agent.dev";

    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>A file of the site's own, served as it is.</summary>
    private static string Served(string file) =>
        Path.Combine(TestPaths.RepoRoot, "src/site/LouisAgent.SiteFramework/wwwroot", file);

    /// <summary>Every page, in the file, and nothing else.</summary>
    [Test]
    public void TheSitemap_ListsEveryPage()
    {
        var expected = Addresses().Select(address => Site + address).ToList();

        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1") Write(expected);
        var listed = XDocument.Load(Served("sitemap.xml")).Descendants(Ns + "loc").Select(loc => loc.Value).ToList();

        Assert.That(listed, Is.EqualTo(expected), "LOUIS_UPDATE_GOLDEN=1 writes the sitemap from the pages");
    }

    /// <summary>The site's robots.txt lets every crawler in and names the sitemap.</summary>
    [Test]
    public void RobotsTxt_NamesTheSitemap() =>
        Assert.That(File.ReadAllText(Served("robots.txt")),
            Does.Contain("User-agent: *").And.Contain($"Sitemap: {Site}/sitemap.xml"));

    /// <summary>
    /// The pages in the menu's order, each by its own address. A section's address answers its page and names the
    /// page as the original (FX216), so a sitemap that listed it would only point Google at a copy.
    /// </summary>
    private static List<string> Addresses()
    {
        var services = new ServiceCollection();
        services.AddSiteAreas(FrameworkAreas.Name);
        using var provider = services.BuildServiceProvider();
        var rule = provider.GetRequiredService<SiteAddresses>();
        return FrameworkContents.All.Pages.Select(entry => rule.Address(entry.Address, null)).Distinct().ToList();
    }

    /// <summary>Writes the sitemap from the pages.</summary>
    private static void Write(IEnumerable<string> addresses)
    {
        var file = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement(Ns + "urlset",
                addresses.Select(address => new XElement(Ns + "url", new XElement(Ns + "loc", address)))));
        file.Save(Served("sitemap.xml"));
    }
}
