using System.Reflection;
using LouisAgent.Api.Site.Places;
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
    /// <paramref name="publicUrl"/> is where the site is served (its canonical links), the address each request came
    /// to when none is given; <paramref name="home"/> is the word its landing's sections sit under, when it has one.
    /// </summary>
    public static WebApplication MapLouisAgentSite(this WebApplication app, Assembly site,
        Action<IServiceCollection> services, Uri? publicUrl = null, string? home = null)
    {
        var routes = SiteRoutes.Of(site, home);
        var prerenderer = new PagePrerenderer(services);
        var handler = new SitePageHandler(routes, prerenderer, publicUrl, app.Environment);
        // The site's places, for the agent's tools to find (F127.7), when this host runs agents.
        app.Services.GetService<AgentEngineFactory>()?.Context.Places.Serve(new SitePlaces(routes, prerenderer));
        app.MapFallback(handler.HandleAsync);
        return app;
    }
}
