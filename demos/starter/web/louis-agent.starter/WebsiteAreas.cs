using LouisAgent.Starter.Layout;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Evals;

namespace LouisAgent.Starter;

/// <summary>
/// What the smallest site is made of (F127.6): the chat portal, the admin portal, the accounts' pages and the eval
/// studio, each named once through the framework's builder. A new site made from this starts here, and adds its packs'
/// views to the admin.
/// </summary>
/// <remarks>
/// Composition Root (Seemann): the one place the site says which parts it uses. The program hands it to
/// <c>AddSiteAreas</c>, and the site's tests hand it the same, so they draw what the site draws.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class WebsiteAreas
{
    /// <summary>
    /// Names the site's areas: a pack's view goes in the admin's, as <c>Admin(layout, views => …)</c>, and the tools
    /// the overview counts in its own, as <c>Overview(count => …)</c>.
    /// </summary>
    public static void Name(SiteAreasBuilder areas) =>
        areas.Chat()
            .Admin(typeof(AdminLayout))
            .Overview()
            .Accounts(typeof(AccountLayout))
            .Evals();
}
