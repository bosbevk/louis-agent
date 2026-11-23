using System.Reflection;
using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Site.Places;
using LouisAgent.Api.Site.Reading;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.Core.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Site;

/// <summary>
/// Serves a site's pages as HTML (F121, Louis's picks: prerender each page, in the framework, in the API on request):
/// the one call a host makes in place of <c>MapFallbackToFile("index.html")</c>. A file still answers as a file; a
/// page's address answers its own rendered HTML, so a search engine that runs no script reads the page.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class SitePages
{
    /// <summary>
    /// Maps every address that isn't a file to the site's pages: <paramref name="site"/> is the site's assembly, whose
    /// routes name its pages; <paramref name="services"/> registers what its pages ask for;
    /// <paramref name="publicUrl"/> is where the site is served (its canonical links), SITE_ADDRESS when none is given,
    /// and the address each request came to when neither is; <paramref name="home"/> is the word its landing's sections
    /// sit under, when it has one.
    /// </summary>
    public static WebApplication MapLouisAgentSite(this WebApplication app, Assembly site,
        Action<IServiceCollection> services, Uri? publicUrl = null, string? home = null)
    {
        // A program may name the site's address; else SITE_ADDRESS does (FX216), read from the host's settings.
        publicUrl ??= SiteAddressOptions.FromEnvironment(name => app.Configuration[name]).Address;
        var routes = SiteRoutes.Of(site, services, home);
        var prerenderer = new PagePrerenderer(services);
        var handler = new SitePageHandler(routes, prerenderer, publicUrl, app.Environment);
        // The site's places, for the agent's tools to find (F127.7), when this host runs agents, and for its search
        // (F164.2), on one index rendered once.
        var places = new SitePlaces(routes, prerenderer);
        app.Services.GetService<AgentEngineFactory>()?.Context.Places.Serve(places);
        var v1 = app.MapHostGroup();
        SitePlaceSearch.Map(v1, places);
        // A page as a message carries it (F126.3.3), for an eval's run to send its agent what the widget would.
        SitePageRoute.Map(v1, ReadPages.Create(routes, prerenderer, services));
        app.MapFallback(handler.HandleAsync);
        return app;
    }
}
