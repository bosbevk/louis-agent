using System.Reflection;
using LouisAgent.Api.Contracts.Site;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Api.Site;

/// <summary>
/// Which of a site's pages an address names (F121), from the routes the pages declare: a page's own address, or the
/// page a section's address sits under (ADR 0107: <c>/proof/rand</c> is on <c>/proof</c>, and the landing's sections
/// under the site's home word, <c>/home/rule</c>, are on <c>/</c>). The site's pages are its own and those its
/// areas bring (F162.1), such as the framework's or the accounts'. A route with a parameter is no page of its own,
/// neither is one under <c>/admin</c> (F142), which sits behind the admin key, nor a page behind a sign-in, which
/// says so with <c>[Authorize]</c> (F162.1): none of these is ever rendered for a search engine or a place the guide
/// sends a visitor.
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
    public static SiteRoutes Of(Assembly site, string? home) => Read([site], home);

    /// <summary>
    /// The site's pages and those its areas bring, which the site's own <paramref name="services"/> name, so a
    /// package a site turns on is drawn without the host being told of it again.
    /// </summary>
    public static SiteRoutes Of(Assembly site, Action<IServiceCollection> services, string? home)
    {
        var named = new ServiceCollection();
        services(named);
        var areaPages = named.Select(service => service.ImplementationInstance).OfType<SitePageAssemblies>()
            .SelectMany(pages => pages.Assemblies);
        return Read([site, .. areaPages], home);
    }

    /// <summary>The public pages of these assemblies, by their routes, the first of a route winning.</summary>
    private static SiteRoutes Read(IEnumerable<Assembly> assemblies, string? home) => new(
        assemblies.Distinct().SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttribute<AuthorizeAttribute>() is null)
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
