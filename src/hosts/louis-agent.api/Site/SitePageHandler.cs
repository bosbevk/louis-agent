using System.Collections.Concurrent;
using System.Text;
using LouisAgent.Composition.Logging;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.Core.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Site;

/// <summary>
/// Answers a site's page address with its HTML (F121): the page rendered once and kept until the process restarts (a
/// deploy), put into the site's index.html at the address asked for. A page that declares no description, or that
/// fails to render, answers the plain shell as before; an address that names no page answers the shell with a 404.
/// </summary>
/// <remarks>
/// One handler class for the endpoint's work (ADR 0051), holding what every request shares: the routes, the renderer,
/// the rendered pages and the shell. A page is rendered once per process, whatever address under it is asked for, so
/// a crawler walking every section of a page costs one render.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
internal sealed class SitePageHandler(SiteRoutes routes, PagePrerenderer prerenderer, Uri? publicUrl,
    IWebHostEnvironment environment)
{
    private static readonly ILogger Log = AgentLogging.For<SitePageHandler>();

    private readonly ConcurrentDictionary<Type, Task<RenderedPage?>> _rendered = new();
    private readonly Lazy<string> _index = new(() => ReadIndex(environment));

    /// <summary>The page's HTML at its address, the plain shell, or the shell with a 404.</summary>
    /// <remarks>
    /// Guard clauses, one case each (ADR 0051). It takes the request, not the context: a method of the context alone
    /// that returns a task is bound as a plain request delegate, and its result would never be written.
    /// </remarks>
    public async Task<ContentHttpResult> HandleAsync(HttpRequest request)
    {
        string address = request.Path.Value ?? "/";
        if (routes.PageFor(address) is not var (route, page)) return Html(_index.Value, StatusCodes.Status404NotFound);

        // The site's public address, or the one this request came to (behind a proxy, its forwarded scheme and host).
        var site = publicUrl ?? new Uri($"{request.Scheme}://{request.Host}/");
        var rendered = await _rendered.GetOrAdd(page, _ => RenderAsync(page, new Uri(site, route)));
        if (rendered is null || rendered.Description.Length == 0) return Html(_index.Value, StatusCodes.Status200OK);

        return Html(PageShell.Fill(_index.Value, rendered, site, address), StatusCodes.Status200OK);
    }

    /// <summary>
    /// A page rendered at its own route; a failure is logged once and gives the plain shell from then on.
    /// </summary>
    private async Task<RenderedPage?> RenderAsync(Type page, Uri at)
    {
        var rendered = await prerenderer.RenderAsync(page, at);
        if (rendered is null)
            Log.Log(LogLevel.Warning, ApiLogMessage.PagePrerenderFailed,
                new { Page = page.Name, Route = at.AbsolutePath });
        return rendered;
    }

    /// <summary>The site's index.html from its web root, read once.</summary>
    private static string ReadIndex(IWebHostEnvironment environment)
    {
        var file = environment.WebRootFileProvider.GetFileInfo("index.html");
        using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>HTML with a status.</summary>
    private static ContentHttpResult Html(string html, int status) =>
        TypedResults.Content(html, "text/html", Encoding.UTF8, status);
}
