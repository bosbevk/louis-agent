using System.Reflection;

namespace LouisAgent.Web.Components.Areas;

/// <summary>
/// The areas a site named (F127.6), registered once: the site's router takes its page assemblies from here, and the
/// admin frame the links each area adds to its sidebar.
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
}
