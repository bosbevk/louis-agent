using System.Reflection;
using System.Xml.Linq;
using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components;
using LouisAgent.Site.Pages;

namespace LouisAgent.Site.Tests.Pages.Meta;

/// <summary>
/// The site's sitemap lists every page by its own address (F119, FX216), so a search engine finds them all and only
/// the originals: a section's address answers its page and names the page as its canonical. The file is held to the
/// pages, and must say the same. <c>LOUIS_UPDATE_GOLDEN=1</c> writes it, as it writes the golden files.
/// </summary>
public class SitemapTests
{
    private const string Site = "https://louis-agent.dev";
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static string SitemapPath => Path.Combine(TestPaths.RepoRoot,
        "src/site/LouisAgent.Site/wwwroot/sitemap.xml");

    /// <summary>Every page, in the file, and nothing else.</summary>
    [Test]
    public void TheSitemap_ListsEveryPage()
    {
        var expected = Addresses().Select(address => Site + address).ToList();

        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1") Write(expected);
        var listed = XDocument.Load(SitemapPath).Descendants(Ns + "loc").Select(loc => loc.Value).ToList();

        Assert.That(listed, Is.EqualTo(expected), "LOUIS_UPDATE_GOLDEN=1 writes the sitemap from the pages");
    }

    /// <summary>The site's robots.txt names the sitemap and lets every crawler in.</summary>
    [Test]
    public void RobotsTxt_NamesTheSitemap()
    {
        string robots = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/LouisAgent.Site/wwwroot/robots.txt"));

        Assert.That(robots, Does.Contain("User-agent: *").And.Contain($"Sitemap: {Site}/sitemap.xml"));
    }

    /// <summary>
    /// The site's pages, each by its own address. A section's address answers its page and names the page as the
    /// original (FX216), so a sitemap that listed it would only point Google at a copy.
    /// </summary>
    private static List<string> Addresses()
    {
        var routes = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .Select(type => type.GetCustomAttributes<RouteAttribute>().FirstOrDefault()?.Template)
            .OfType<string>()
            .OrderBy(route => route == "/" ? "" : route, StringComparer.Ordinal)
            .ToList();
        var rule = new SiteAddresses("home", routes);
        return routes.Select(route => rule.Address(route, null)).Distinct().ToList();
    }

    /// <summary>Writes the sitemap from the pages.</summary>
    private static void Write(IEnumerable<string> addresses)
    {
        var file = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement(Ns + "urlset",
                addresses.Select(address => new XElement(Ns + "url", new XElement(Ns + "loc", address)))));
        file.Save(SitemapPath);
    }
}
