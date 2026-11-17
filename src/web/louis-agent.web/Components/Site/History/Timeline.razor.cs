using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.History;

/// <summary>
/// A timeline, newest first, such as the jobs on a CV or the courses after them: each entry its dates in a column
/// beside its words, one under the other on a phone, and kept whole on a printed page.
/// </summary>
/// <remarks>
/// Composite, in Razor, as <see cref="Tiles.TileRow"/> is: the timeline only lays out what it holds, and each
/// <see cref="TimelineEntry"/> draws itself, so a site says what happened when and never how the column is drawn.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Timeline
{
    /// <summary>A line between the entries and room around each, as jobs have; a short list of rows has none.</summary>
    [Parameter] public bool Ruled { get; set; } = true;

    /// <summary>The entries, and anything a site puts after them, such as a button that adds one.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The timeline's classes: ruled, or close rows.</summary>
    private string Classes => Ruled ? "timeline is-ruled" : "timeline";
}
