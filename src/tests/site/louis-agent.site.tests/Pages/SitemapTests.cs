using System.Reflection;
using System.Xml.Linq;
using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components;
using LouisAgent.Site.Pages;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// The site's sitemap lists every page and every place on it by its own address (F119, ADR 0107), so a search engine
/// finds them all: search engines read a page without what follows a <c>#</c>, and only real addresses can be listed.
/// The file is held to the pages: every site page is rendered, its sections and cards collected by the address rule,
/// and the file must say the same. <c>LOUIS_UPDATE_GOLDEN=1</c> writes it, as it writes the golden files.
/// </summary>
public class SitemapTests
{
    private const string Site = "https://louis-agent.dev";
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static string SitemapPath => Path.Combine(TestPaths.RepoRoot,
        "src/site/louis-agent.site/wwwroot/sitemap.xml");

    /// <summary>Every page and place, in the file, and nothing else.</summary>
    [Test]
    public void TheSitemap_ListsEveryPageAndPlace()
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
            "src/site/louis-agent.site/wwwroot/robots.txt"));

        Assert.That(robots, Does.Contain("User-agent: *").And.Contain($"Sitemap: {Site}/sitemap.xml"));
    }

    /// <summary>
    /// The site's pages and their places, in order: each page's own address, then its sections and cards by the rule,
    /// the first section being the page itself.
    /// </summary>
    private static List<string> Addresses()
    {
        var pages = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .Select(type => (Type: type, Route: type.GetCustomAttributes<RouteAttribute>().FirstOrDefault()?.Template))
            .Where(page => page.Route is not null)
            .OrderBy(page => page.Route == "/" ? "" : page.Route, StringComparer.Ordinal)
            .ToList();
        var rule = new SiteAddresses("home", pages.Select(page => page.Route!));

        using var ctx = SitePageContext.Create();
        var addresses = new List<string>();
        foreach (var (type, route) in pages)
        {
            var cut = ctx.Render(builder =>
            {
                builder.OpenComponent(0, type);
                builder.CloseComponent();
            });
            addresses.Add(rule.Address(route!, null));
            addresses.AddRange(cut.FindAll("section[id], [data-card][id]")
                .Select(place => place.Id)
                .Where(id => id != SiteAddresses.Top)
                .Select(id => rule.Address(route!, id)));
        }

        return addresses.Distinct().ToList();
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
