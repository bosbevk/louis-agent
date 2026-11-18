using System.Reflection;

namespace LouisAgent.Api.Contracts.Site;

/// <summary>
/// The assemblies whose pages a site's areas bring, such as the framework's pages or the accounts': the site's web
/// registers it, and the host reads it to draw those pages for a search engine as it draws the site's own.
/// </summary>
/// <remarks>
/// A shared contract (F162.1, Louis's pick A of three): the web knows the areas and the host knows how to draw a
/// page, and neither references the other, so they agree through this record in the contracts both already use,
/// rather than the host referencing the web or every site's host naming its packages by hand.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public sealed record SitePageAssemblies(IReadOnlyList<Assembly> Assemblies);
