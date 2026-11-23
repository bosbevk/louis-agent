using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Evals.Admin;
using LouisAgent.Web.Evals.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LouisAgent.Web.Evals;

/// <summary>
/// How a site takes the eval pages: one word among its areas, <c>.Evals()</c>, gives the router the pages, the
/// services the client, the admin's sidebar its link and its home a card. A site that names no evals ships none of
/// it, as a site that maps no eval routes on its API runs none.
/// </summary>
/// <remarks>The repo's own area, as the accounts and the docs are (ADR 0084, ADR 0120).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public static class EvalsArea
{
    /// <summary>The eval pages, named among the site's areas.</summary>
    public static SiteAreasBuilder Evals(this SiteAreasBuilder areas) =>
        areas.Add(new SiteArea("evals", services => services.TryAddScoped<EvalsApi>(), [EvalsLibrary.Assembly],
            AdminLinks: typeof(EvalsLinks), AdminCard: typeof(EvalsCard)));
}
