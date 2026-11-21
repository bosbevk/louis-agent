using LouisAgent.Site;
using LouisAgent.Site.Resumes.Sources;
using LouisAgent.SiteApi.Resumes;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Text;

namespace LouisAgent.SiteApi;

/// <summary>
/// What the site's pages ask for when the host renders them itself (F121): the HTML a search engine reads, before
/// the app starts in a browser.
/// </summary>
/// <remarks>
/// Composition Root, as <see cref="SiteCatalog"/> is. A class of its own so the program and its test share one
/// list: a page drawn in a test is then drawn with what the host really hands it. That matters because the site's
/// own program registers the same things for the browser, by hand, and this list once fell behind it: the host drew
/// the showroom for a search engine with no demos in it (FX155).
/// </remarks>
public static class SitePageServices
{
    /// <summary>
    /// Registers what a page the host renders asks for. The CV comes from <paramref name="resumes"/>.
    /// </summary>
    public static void Register(IServiceCollection services, ResumeStore resumes)
    {
        services.AddSingleton(new ChatText());
        services.AddSingleton(new HttpClient());
        // A page rendered here reads the CV from the store, where the browser asks the API.
        services.AddSingleton<IResumeSource>(new StoredResumeSource(resumes));

        // As the site's own program registers them: the site's areas, so the showroom has its demos and a page knows
        // which place it was asked at.
        services.AddSiteAreas(LouisAgentDevAreas.Name);
    }
}
