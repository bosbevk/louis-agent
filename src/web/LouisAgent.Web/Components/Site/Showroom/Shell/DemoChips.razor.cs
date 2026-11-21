using LouisAgent.Choices;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Shell;

/// <summary>
/// The showroom's demos on a phone (F127.1): the rail's demos as a row of chips that scrolls sideways under the bar,
/// each in its colour, the one on screen lit and marked as the current page.
/// </summary>
public partial class DemoChips
{
    /// <summary>The site's demos.</summary>
    [Parameter, EditorRequired]
    public ShowroomDemos Demos { get; set; } = null!;

    /// <summary>The demo on screen; none on the start.</summary>
    [Parameter]
    public ShowroomDemo? Current { get; set; }

    /// <summary>The showroom's words.</summary>
    [Parameter, EditorRequired]
    public ShowroomText Text { get; set; } = null!;

    /// <summary>A chip's classes: its colour, and lit when it's on screen.</summary>
    private string ClassesOf(ShowroomDemo demo) =>
        $"demo-chip accent-{demo.Accent.Wire()}{(demo == Current ? " is-current" : "")}";

    /// <summary>Marks the demo on screen as the current page; no mark on the others.</summary>
    private string? CurrentMark(ShowroomDemo demo) => demo == Current ? "page" : null;
}
