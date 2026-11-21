using LouisAgent.Site.Content;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Sources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Pages.About;

/// <summary>
/// Louis's CV at <c>/cv</c> (F142). It draws the CV the site was built with at once, then the copy its source holds:
/// the site's API in the browser, so an edit saved in the admin shows, and the host's own store on the server, so a
/// search engine reads the same.
/// </summary>
/// <remarks>
/// The page shows what it last fetched (ADR 0042) and keeps no copy of its own beyond the built-in start, which is
/// why it is never empty while the API is asleep.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class CvPage
{
    private Resume _resume = Cv.Resume;

    [Inject] private HttpClient Http { get; set; } = null!;
    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>Asks the host's source when there is one (the server), else the site's API (the browser).</summary>
    protected override async Task OnInitializedAsync()
    {
        var source = Services.GetService<IResumeSource>() ?? new ApiResumeSource(Http);
        _resume = await source.ReadAsync();
    }
}
