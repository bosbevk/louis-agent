using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework;
using LouisAgent.Web.Places;
using LouisAgent.Web.Themes.Flat;

namespace LouisAgent.SiteFramework;

/// <summary>
/// What the framework's own site is made of: the flat theme alone, so the site always opens flat and the visitor picks
/// only light or dark (Louis: "this ones default will be the flat white theme", then "this must always load flat so no
/// glass theme here yet"), and the framework's pages, the package it turns on as a site turns on the accounts. The
/// agent in the corner joins in F164.2.
/// </summary>
/// <remarks>
/// Composition Root (Seemann): the one place the site says which parts it uses. The browser's program, the host's
/// page list and the tests hand it to <c>AddSiteAreas</c>, so they all draw what the site draws.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public static class FrameworkAreas
{
    /// <summary>The word the Overview's places sit under, as in <c>/overview/the-engine</c>.</summary>
    public const string Home = "overview";

    /// <summary>The framework's repository on GitHub, where the doc links lead and the footer's GitHub link goes.</summary>
    public const string Repository = "bosbevk/louis-agent";

    /// <summary>The repository's page on GitHub.</summary>
    public const string RepositoryAddress = "https://github.com/" + Repository;

    /// <summary>
    /// Names the site's areas: flat, the framework's pages, where their doc links lead, the repo on GitHub
    /// until the docs have a site of their own, and every place on the pages with its own address, the Overview's
    /// under its home word.
    /// </summary>
    public static void Name(SiteAreasBuilder areas) =>
        areas.Flat().Framework().Docs(DocsAddress.GitHub(Repository, "main"))
            .Places(Home, typeof(App).Assembly);
}
