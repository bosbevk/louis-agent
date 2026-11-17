using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.History;

/// <summary>
/// One entry of a <see cref="Timeline"/>: when, with a word such as "current" under the dates while it still runs,
/// then its title, where it was, and what a site says of it. Buttons that change the entry sit under its dates.
/// </summary>
/// <remarks>
/// Template Method, in Razor, as <see cref="Hero.SiteHero"/> is: the layout is fixed here and a site fills the
/// slots, so the words may be plain or edited in place and the entry looks the same either way.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class TimelineEntry
{
    /// <summary>When it was, such as "2021 to now".</summary>
    [Parameter, EditorRequired] public RenderFragment? When { get; set; }

    /// <summary>The word under the dates while it still runs; none once it is over.</summary>
    [Parameter] public string? Current { get; set; }

    /// <summary>The buttons that change the entry, under its dates.</summary>
    [Parameter] public RenderFragment? Tools { get; set; }

    /// <summary>What it was: a job's title, a course's name.</summary>
    [Parameter, EditorRequired] public RenderFragment? Title { get; set; }

    /// <summary>Where it was, quiet under the title.</summary>
    [Parameter] public RenderFragment? Place { get; set; }

    /// <summary>What a site says of it: a list of what was done, the tools it used.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
