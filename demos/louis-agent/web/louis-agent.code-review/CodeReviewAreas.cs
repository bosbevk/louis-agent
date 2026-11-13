using LouisAgent.CodeReview.Layout;
using LouisAgent.Packs.Files.Web;
using LouisAgent.Packs.Git.Web;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Evals;

namespace LouisAgent.CodeReview;

/// <summary>
/// What the code-review app is made of (F127.6): the chat portal, the admin portal with the git and files packs' own
/// views, the accounts' pages and the eval studio, each named once through the framework's builder.
/// </summary>
/// <remarks>
/// Composition Root (Seemann): the one place the app says which parts it uses. The program hands it to
/// <c>AddSiteAreas</c>, and the app's tests hand it the same, so they draw what the app draws, not a copy of its
/// setup.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class CodeReviewAreas
{
    /// <summary>Names the app's areas: a screen's Review button opens the chat with a draft.</summary>
    public static void Name(SiteAreasBuilder areas) =>
        areas.Chat()
            .Admin(typeof(AdminLayout))
            .Overview()
            .Packs(PackViewsOf)
            .Accounts(typeof(AccountLayout))
            .Evals();

    /// <summary>Which component draws each screen: the plain table, then the git and files packs' own.</summary>
    private static void PackViewsOf(LouisAgent.Web.Components.Packs.PackViews views)
    {
        GitWebViews.AddTo(views);
        FilesWebViews.AddTo(views);
    }
}
