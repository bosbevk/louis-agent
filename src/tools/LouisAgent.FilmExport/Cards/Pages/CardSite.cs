using System.Xml.Linq;

namespace LouisAgent.FilmExport.Cards.Pages;

/// <summary>
/// A site the card tool draws page cards for: the host project it starts, the web root the cards are written into, and
/// the sitemap its pages are read from, each a path from the repo's root.
/// </summary>
/// <remarks>
/// The pages come from the site's own sitemap, which a test of the site already holds to its pages, so the tool keeps
/// no list of its own that could drift.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record CardSite(string Host, string WebRoot, string Sitemap)
{
    /// <summary>The framework's own site, framework.louis-agent.dev.</summary>
    public static readonly CardSite Framework = new("src/site/LouisAgent.SiteFrameworkApi",
        "src/site/LouisAgent.SiteFramework/wwwroot", "src/site/LouisAgent.SiteFramework/wwwroot/sitemap.xml");

    /// <summary>Each page a sitemap lists, with the address its card shows.</summary>
    public static IReadOnlyList<SitePage> ReadPages(string sitemap)
    {
        XNamespace urls = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = XDocument.Parse(sitemap.TrimStart('﻿'));
        return document.Descendants(urls + "loc").Select(location => PageAt(new Uri(location.Value.Trim()))).ToList();
    }

    /// <summary>A page's route, and its address with no scheme, the root's with no trailing slash.</summary>
    private static SitePage PageAt(Uri address) =>
        new(address.AbsolutePath, (address.Host + address.AbsolutePath).TrimEnd('/'));
}
