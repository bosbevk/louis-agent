using LouisAgent.FilmExport.Cards.Pages;

namespace LouisAgent.FilmExport.Tests.Cards.Pages;

/// <summary>
/// The card tool learns a site's pages from the site's own sitemap (F169.6), which a test already holds to the pages,
/// and knows where the framework's site is in the repo.
/// </summary>
public class CardSiteTests
{
    private const string Sitemap =
        "﻿<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        + "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n"
        + "  <url><loc>https://framework.louis-agent.dev/</loc></url>\n"
        + "  <url><loc>https://framework.louis-agent.dev/getting-started</loc></url>\n"
        + "</urlset>";

    /// <summary>
    /// Each page gives its route and the address its card shows: the host and the path with no scheme, the root's with
    /// no trailing slash.
    /// </summary>
    [Test]
    public void ASitemap_GivesEachPagesRouteAndAddress()
    {
        var pages = CardSite.ReadPages(Sitemap);

        Assert.That(pages, Is.EqualTo(new[]
        {
            new SitePage("/", "framework.louis-agent.dev"),
            new SitePage("/getting-started", "framework.louis-agent.dev/getting-started")
        }));
    }

    /// <summary>The framework's site names its host, its web root and its sitemap, each a path in the repo.</summary>
    [Test]
    public void TheFrameworksSite_NamesPathsThatExist()
    {
        var site = CardSite.Framework;

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(Path.Combine(TestPaths.RepoRoot, site.Host)), site.Host);
            Assert.That(Directory.Exists(Path.Combine(TestPaths.RepoRoot, site.WebRoot)), site.WebRoot);
            Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, site.Sitemap)), site.Sitemap);
            Assert.That(site.Host, Does.EndWith("LouisAgent.SiteFrameworkApi"));
        });
    }
}
