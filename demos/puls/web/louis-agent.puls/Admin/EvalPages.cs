using LouisAgent.Puls.Admin.Explorer.Sites;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Admin;

/// <summary>
/// What the eval pages need from a site's services. A site calls this once: every site has the pages' addresses, so
/// without it opening <c>/admin</c> would crash on a missing service.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class EvalPages
{
    /// <summary>Registers them, with how this site sorts the websites its agent reads, if it does.</summary>
    public static void AddTo(IServiceCollection services, SiteKinds? kinds = null)
    {
        services.AddScoped<ReviewerState>();
        services.AddScoped<ReviewQueueState>();
        services.AddScoped<RunNotesState>();
        services.AddSingleton(kinds ?? SiteKinds.None);
    }
}
