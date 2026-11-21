using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Evals.Admin.Explorer.Sites;
using LouisAgent.Web.Evals.Components.Head;

namespace LouisAgent.Web.Evals;

/// <summary>
/// The eval studio as an area a site names (F127.6): <c>.Evals()</c> registers its services and its pages, and gives
/// the admin frame its links and (F144) its card on the home, so every site that runs the evals links them.
/// </summary>
/// <remarks>
/// An extension on the framework's <see cref="SiteAreasBuilder"/>, from this library, since the framework's web can't
/// name the eval studio (dependencies point one way); it calls <see cref="EvalServices.AddLouisAgentEvals"/>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class EvalArea
{
    /// <summary>The eval studio, for a site of these kinds; none, and the kinds are none.</summary>
    public static SiteAreasBuilder Evals(this SiteAreasBuilder areas, SiteKinds? kinds = null) =>
        areas.Add(new SiteArea("evals", services => services.AddLouisAgentEvals(kinds), [EvalLibrary.Assembly],
            AdminLinks: typeof(EvalLinks), AdminCard: typeof(EvalsCard)));
}
