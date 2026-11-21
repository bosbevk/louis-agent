using System.Reflection;
using LouisAgent.Web.Components.Areas.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Areas;

/// <summary>
/// The areas a site named (F127.6), registered once: the site's router takes its page assemblies from here, the
/// admin frame the links each area adds to its sidebar, and the site its themes, the first named its default.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public sealed class SiteAreas(IReadOnlyList<SiteArea> areas)
{
    /// <summary>The areas' names, in the order the site named them.</summary>
    public IReadOnlyList<string> Names { get; } = [.. areas.Select(area => area.Name)];

    /// <summary>The assemblies the areas' pages live in, each once, for the site's router.</summary>
    public Assembly[] PageAssemblies { get; } = [.. areas.SelectMany(area => area.Pages).Distinct()];

    /// <summary>The components the admin frame draws under the pack menu, one per area that has links.</summary>
    public IReadOnlyList<Type> AdminLinks { get; } = [.. areas.Select(area => area.AdminLinks).OfType<Type>()];

    /// <summary>The cards the admin's home draws (F144), one per area that has one, in the order named.</summary>
    public IReadOnlyList<Type> AdminCards { get; } = [.. areas.Select(area => area.AdminCard).OfType<Type>()];

    /// <summary>The themes a visitor can pick between (F151.1), one per area that brings one, as named.</summary>
    public IReadOnlyList<SiteTheme> Themes { get; } = [.. areas.Select(area => area.Theme).OfType<SiteTheme>()];

    /// <summary>The theme a first visit gets: the first one the site named, or none when it names none.</summary>
    public SiteTheme? DefaultTheme => Themes.Count > 0 ? Themes[0] : null;

    /// <summary>
    /// Whether the site offers a theme a visitor can pick (F151.3), which the frames ask before they make room for the
    /// look switch. A frame drawn with no areas named offers none.
    /// </summary>
    public static bool OffersThemes(IServiceProvider services) => services.GetService<SiteAreas>()?.Themes.Count > 0;
}
