using System.Reflection;
using System.Xml.Linq;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Framework;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.SiteFramework.Tests.Meta;

/// <summary>
/// The framework's site lists every page and every place on it by its own address (F162.1, step 12, ADR 0107), in the
/// menu's reading order, so a search engine finds them all; its robots.txt lets every crawler in and names the
/// sitemap. The file is held to the pages: each is drawn, its places collected by the address rule, and the file must
/// say the same. <c>LOUIS_UPDATE_GOLDEN=1</c> writes it, as it writes the golden files.
/// </summary>
public class SitemapTests
{
    /// <summary>The site's address on the web.</summary>
    private const string Site = "https://framework.louis-agent.dev";

    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>A file of the site's own, served as it is.</summary>
    private static string Served(string file) =>
        Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site-framework/wwwroot", file);

    /// <summary>Every page and place, in the file, and nothing else.</summary>
    [Test]
    public void TheSitemap_ListsEveryPageAndPlace()
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
    /// The pages in the menu's order, each its own address, then its places by the rule, the first section being the
    /// page itself.
    /// </summary>
    private static List<string> Addresses()
    {
        var pages = FrameworkLibrary.Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>().Select(route => (route.Template, type)))
            .ToDictionary(page => page.Template, page => page.type);

        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSiteAreas(FrameworkAreas.Name);
        var rule = ctx.Services.GetRequiredService<SiteAddresses>();
        var addresses = new List<string>();
        foreach (var entry in FrameworkContents.All.Pages)
        {
            var cut = ctx.Render(builder =>
            {
                builder.OpenComponent(0, pages[entry.Address]);
                builder.CloseComponent();
            });
            addresses.Add(rule.Address(entry.Address, null));
            addresses.AddRange(cut.FindAll("section[id]").Select(place => place.Id)
                .Where(id => id != SiteAddresses.Top).Select(id => rule.Address(entry.Address, id)));
        }

        return addresses.Distinct().ToList();
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
