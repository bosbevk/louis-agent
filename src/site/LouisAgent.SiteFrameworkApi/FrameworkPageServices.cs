using LouisAgent.SiteFramework;
using LouisAgent.Web.Components.Areas;

namespace LouisAgent.SiteFrameworkApi;

/// <summary>
/// What the framework's site's pages ask for when the host draws them itself: the HTML a search engine reads, before
/// the app starts in a browser.
/// </summary>
/// <remarks>
/// Composition Root, beside the site's own <see cref="FrameworkAreas"/>. A class of its own so the program and its
/// test share one list: a page drawn in a test is drawn with what the host really hands it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class FrameworkPageServices
{
    /// <summary>Registers what a page the host draws asks for, as the site's own program registers it.</summary>
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton(new HttpClient());
        // The site's areas, every place's address among them, so a page knows which place it was asked at.
        services.AddSiteAreas(FrameworkAreas.Name);
    }
}
