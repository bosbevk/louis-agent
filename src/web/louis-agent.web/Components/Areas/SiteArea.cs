using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Areas;

/// <summary>
/// One area a site uses (F127.6): what it registers, the assemblies its pages live in, and what it adds to the admin
/// frame's sidebar and the account pages. A library above the framework's web describes its own area with one.
/// </summary>
/// <param name="Name">How the site's areas list it; a name given twice counts once.</param>
/// <param name="Services">What the area registers.</param>
/// <param name="Pages">The assemblies its pages live in, for the site's router.</param>
/// <param name="AdminLinks">A component the admin frame draws under the pack menu, if the area has links.</param>
/// <param name="AccountLayout">The layout the account pages draw inside, for the accounts area.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public sealed record SiteArea(
    string Name,
    Action<IServiceCollection> Services,
    IReadOnlyList<Assembly> Pages,
    Type? AdminLinks = null,
    Type? AccountLayout = null);
