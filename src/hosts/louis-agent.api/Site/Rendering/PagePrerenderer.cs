using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace LouisAgent.Api.Site.Rendering;

/// <summary>
/// Renders a site's page to HTML on the server (F121, Louis's pick: in the API, on request), so a search engine that
/// runs no script gets the page's own title, description and words. The page renders beside a head outlet with
/// ASP.NET Core's own <see cref="HtmlRenderer"/>; its scripts and <c>OnAfterRender</c> never run, which leaves what the
/// page says before the app starts: all its words.
/// </summary>
/// <remarks>
/// The site hands over the services its pages ask for (one callback, as its own program registers them); the
/// framework adds the address the page is rendered at and a script runtime that answers with nothing. A page that
/// fails gives null, and the host serves the plain shell instead, so a prerender can never break a site.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public sealed class PagePrerenderer(Action<IServiceCollection> siteServices)
{
    /// <summary>The page at the address as HTML, or null when it couldn't be rendered.</summary>
    public async Task<RenderedPage?> RenderAsync(Type page, Uri address)
    {
        // The services a page may ask for, fresh for this address: the site's, then where it is and no scripts.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        siteServices(services);
        services.AddSingleton<NavigationManager>(new StaticNavigation(address));
        services.AddSingleton<IJSRuntime>(NoScripts.Instance);

        try
        {
            await using var provider = services.BuildServiceProvider();
            await using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);
            string html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(
                    new Dictionary<string, object?> { [nameof(PrerenderRoot.PageType)] = page });
                return (await renderer.RenderComponentAsync<PrerenderRoot>(parameters)).ToHtmlString();
            });
            return PrerenderedHtml.Read(html);
        }
        // Whatever the page threw, the visitor still gets the site: the host falls back to the plain shell.
        catch (Exception)
        {
            return null;
        }
    }
}
