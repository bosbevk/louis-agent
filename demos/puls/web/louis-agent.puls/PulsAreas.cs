using LouisAgent.Packs.Recipes.Web;
using LouisAgent.Puls.Content;
using LouisAgent.Puls.Layout;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Evals;

namespace LouisAgent.Puls;

/// <summary>
/// What Puls is made of (F127.6): the admin portal with the recipes pack's own views, the accounts' pages and the eval
/// studio with Puls's sites, each named once through the framework's builder. Its chat is its own pages, so it names
/// no chat portal.
/// </summary>
/// <remarks>
/// Composition Root (Seemann): the one place the site says which parts it uses. The program hands it to
/// <c>AddSiteAreas</c>, and the site's tests hand it the same, so they draw what the site draws.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class PulsAreas
{
    /// <summary>Names Puls's areas.</summary>
    public static void Name(SiteAreasBuilder areas) =>
        areas.Admin(typeof(AdminLayout), PackViewsOf)
            .Overview()
            .Accounts(typeof(AccountLayout))
            .Evals(PulsSites.Kinds);

    /// <summary>Which component draws each screen: the plain table, then the recipes pack's own.</summary>
    private static void PackViewsOf(PackViews views) => RecipeWebViews.AddTo(views);
}
