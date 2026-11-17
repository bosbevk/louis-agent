using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Framework;
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

    /// <summary>Names the site's areas: flat, then glass, and the framework's pages.</summary>
    public static void Name(SiteAreasBuilder areas) => areas.Flat().Glass().Framework();

    /// <summary>
    /// Every place's address on the site's pages, its own and the framework package's, so a page knows which place it
    /// was asked at. The browser's program and the host's page list both read it.
    /// </summary>
    public static SiteAddresses Addresses() => SiteAddresses.From(Home, typeof(App).Assembly, FrameworkLibrary.Assembly);
}
