using LouisAgent.Site.Components.Showroom;
using LouisAgent.Web.Components.Areas;

namespace LouisAgent.Site;

/// <summary>
/// What louis-agent.dev is made of (F127.6): its showroom, with the site's own demos, named through the framework's
/// builder; the guide in its corner runs on the chat's plumbing, which every site gets. The framework's site has no
/// chat portal and no admin yet (F127.7, F127.8 name them here).
/// </summary>
/// <remarks>
/// Composition Root (Seemann): the one place the site says which parts it uses. The program hands it to
/// <c>AddSiteAreas</c>, and the site's tests hand it the same, so they draw what the site draws.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class LouisAgentDevAreas
{
    /// <summary>Names the site's areas: the showroom, the rail and the covers drawn from its demos.</summary>
    public static void Name(SiteAreasBuilder areas) => areas.Showroom(SiteDemos.All);
}
