using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework;
using LouisAgent.Web.Places;
using LouisAgent.Web.Themes.Flat;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.SiteFramework;

/// <summary>
/// What the framework's own site is made of: the two themes, flat first so the site opens flat and light (Louis:
/// "this ones default will be the flat white theme"), glass offered beside it, and the framework's pages, the
/// package it turns on as a site turns on the accounts. The agent in the corner joins in F160.2.
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

    /// <summary>
    /// Names the site's areas: flat, then glass, the framework's pages, where their doc links lead, the repo on GitHub
    /// until the docs have a site of their own, and every place on the pages with its own address, the Overview's
    /// under its home word.
    /// </summary>
    public static void Name(SiteAreasBuilder areas) =>
        areas.Flat().Glass().Framework().Docs(DocsAddress.GitHub("bosbevk/louis-agent", "main"))
            .Places(Home, typeof(App).Assembly);
}
