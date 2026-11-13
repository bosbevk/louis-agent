using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Api.Site;

/// <summary>
/// Which of a site's pages an address names (F121), from the routes the pages declare: a page's own address, or the
/// page a section's address sits under (ADR 0107: <c>/proof/rand</c> is on <c>/proof</c>, and the landing's sections
/// under the site's home word, <c>/home/rule</c>, are on <c>/</c>). A route with a parameter is no page of its own,
/// and neither is one under <c>/admin</c> (F142): an admin page a site keeps in its own project sits behind the
/// admin key, so it is never rendered for a search engine and never a place the guide sends a visitor.
/// </summary>
public sealed class SiteRoutes
{
    /// <summary>Where every site's admin lives, the frame's home and each area's pages under it (F144).</summary>
    private const string Admin = "/admin";

    private readonly Dictionary<string, Type> _pages;
    private readonly string? _home;

    private SiteRoutes(Dictionary<string, Type> pages, string? home) => (_pages, _home) = (pages, home);

    /// <summary>
    /// The site's pages by their routes, read from its assembly; the home word for the landing's sections.
    /// </summary>
    public static SiteRoutes Of(Assembly site, string? home) => new(
        site.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>().Select(route => (route.Template, type)))
            .Where(route => !route.Template.Contains('{') && !InTheAdmin(route.Template))
            .GroupBy(route => Normal(route.Template), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().type, StringComparer.OrdinalIgnoreCase),
        home);

    /// <summary>Every page of the site by its route, in the order the site declares them.</summary>
    public IEnumerable<(string Route, Type Page)> Pages => _pages.Select(page => (page.Key, page.Value));

    /// <summary>The address a place on this page sits under: the landing's under the home word (ADR 0107).</summary>
    public string PlacesUnder(string route) => route == "/" ? "/" + (_home ?? "") : route;

    /// <summary>The page an address names and its route, or null when it names none.</summary>
    /// <remarks>
    /// Guard clauses, one case each (ADR 0051): the page itself, a page above it, the landing's home word.
    /// </remarks>
    public (string Route, Type Page)? PageFor(string address)
    {
        string path = Normal(address);
        if (_pages.TryGetValue(path, out var exact)) return (path, exact);

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int count = segments.Length - 1; count >= 1; count--)
        {
            string above = "/" + string.Join('/', segments.Take(count));
            if (_pages.TryGetValue(above, out var page)) return (above, page);
        }

        bool underHome = _home is not null && segments.Length > 0
                                           && string.Equals(segments[0], _home, StringComparison.OrdinalIgnoreCase);
        return underHome && _pages.TryGetValue("/", out var landing) ? ("/", landing) : null;
    }

    /// <summary>Whether a route is the admin's home or sits under it.</summary>
    private static bool InTheAdmin(string template)
    {
        string route = Normal(template);
        return route.Equals(Admin, StringComparison.OrdinalIgnoreCase) ||
               route.StartsWith(Admin + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>An address as a route: a leading slash, no trailing one.</summary>
    private static string Normal(string address) => "/" + address.Trim('/');
}
