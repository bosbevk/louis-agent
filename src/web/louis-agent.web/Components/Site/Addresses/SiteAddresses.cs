using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Addresses;

/// <summary>
/// Every place on a site's page has its own address (F118, ADR 0107): a section, <c>/pricing/plans</c>; a card,
/// <c>/pricing/basic</c>; a page of a section paged sideways, <c>/about/team/2</c>; the landing's under the site's
/// home word, <c>/home/welcome</c>, so they never clash with a page of the same name. This class is that rule,
/// both ways: it writes a place's address and reads an address back into the page, the place and the page of it. The
/// guide, the index, the address bar and the router all go through it.
/// </summary>
/// <remarks>
/// One small class for the rule and the site's pages, built once from the routes the pages already declare, so no
/// page is edited to give its sections addresses and a new page has them by construction.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public sealed class SiteAddresses
{
    private readonly string _home;
    private readonly Dictionary<string, Type?> _pages;

    /// <summary>The site's home word and its pages' addresses.</summary>
    public SiteAddresses(string home, IEnumerable<string> pages)
        : this(home, pages.Select(page => (page, (Type?)null)))
    {
    }

    /// <summary>The site's home word and its pages, each with the component that draws it.</summary>
    private SiteAddresses(string home, IEnumerable<(string Page, Type? Type)> pages)
    {
        _home = home.Trim('/').ToLowerInvariant();
        _pages = pages.GroupBy(page => Clean(page.Page))
            .ToDictionary(group => group.Key, group => group.First().Type, StringComparer.Ordinal);
    }

    /// <summary>The addresses from the pages' own routes in the given assemblies, the fixed ones only.</summary>
    public static SiteAddresses From(string home, params Assembly[] assemblies) =>
        new(home, assemblies.SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>().Select(route => (route.Template, (Type?)type)))
            .Where(route => !route.Template.Contains('{')));

    /// <summary>The site's pages' addresses.</summary>
    public IReadOnlyCollection<string> Pages => _pages.Keys;

    /// <summary>The component that draws a page, when the addresses came from the pages' routes.</summary>
    public Type? PageType(string page) => _pages.GetValueOrDefault(Clean(page));

    /// <summary>What a place's address starts with on a page: the page's own, or the home word on the landing.</summary>
    public string Base(string page)
    {
        string clean = Clean(page);
        return clean == "/" ? $"/{_home}" : clean;
    }

    /// <summary>A place's address, or the page's own when no place is named.</summary>
    public string Address(string page, string? section) =>
        string.IsNullOrEmpty(section) ? Clean(page) : $"{Base(page)}/{section}";

    /// <summary>
    /// What an address points to, or none when it names no page of the site. Read from the end: a number is the page
    /// of a paged section, the word before it the place, and what is left the page.
    /// </summary>
    public SiteAddress? Read(string address)
    {
        string clean = Clean(address);
        if (clean == $"/{_home}") return new SiteAddress("/");
        if (_pages.ContainsKey(clean)) return new SiteAddress(clean);

        var parts = clean.Trim('/').Split('/').ToList();
        int? turn = parts.Count > 0 && int.TryParse(parts[^1], out int number) && number >= 1 ? number : null;
        if (turn is not null) parts.RemoveAt(parts.Count - 1);
        if (parts.Count < 2 || !IsSectionId(parts[^1])) return null;

        string section = parts[^1];
        string page = "/" + string.Join('/', parts.Take(parts.Count - 1));
        if (page == $"/{_home}") return new SiteAddress("/", section, turn);
        return _pages.ContainsKey(page) ? new SiteAddress(page, section, turn) : null;
    }

    /// <summary>A place's id: a letter first, then letters, digits and dashes.</summary>
    private static bool IsSectionId(string id) =>
        id.Length > 0 && char.IsAsciiLetterLower(id[0]) &&
        id.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

    /// <summary>An address in one form: lowercase, one leading slash, no trailing one, no query or fragment.</summary>
    private static string Clean(string address)
    {
        string path = address.Split('?', '#')[0].Trim().ToLowerInvariant().TrimEnd('/');
        if (path.Length == 0) return "/";
        return path.StartsWith('/') ? path : "/" + path;
    }
}
