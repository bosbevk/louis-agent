using LouisAgent.Web.Evals.Admin.Explorer.Sites;
using LouisAgent.Web.Evals.Admin.Notes;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.Reviews.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals;

/// <summary>
/// What a site adds to have the eval studio (CL57, ADR 0092): the state the review, the review queue and a run's
/// notes keep across pages, and the kinds of site the explorer tells apart. One line in a site's program, beside
/// <c>AddLouisAgentAccounts</c>; the router takes <see cref="EvalPages.Assembly"/> too.
/// </summary>
/// <remarks>
/// Extension Method, the way ASP.NET itself is put together, so a site's program reads as the app it is and the
/// framework's web knows nothing of the studio.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0092-a-project-holds-what-is-reused-together.md">ADR 0092: a project holds what is reused together</seealso>
public static class EvalServices
{
    /// <summary>The studio's services; the kinds of site are a site's own, none by default.</summary>
    public static IServiceCollection AddLouisAgentEvals(this IServiceCollection services, SiteKinds? kinds = null)
    {
        services.AddScoped<ReviewerState>();
        services.AddScoped<ReviewQueueState>();
        services.AddScoped<RunNotesState>();
        services.AddSingleton(kinds ?? SiteKinds.None);
        return services;
    }
}
