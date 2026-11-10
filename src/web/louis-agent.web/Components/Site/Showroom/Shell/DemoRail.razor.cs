using LouisAgent.Choices;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Shell;

/// <summary>
/// Every demo of the showroom down its left side (F127.1), grouped by what a visitor comes for, so they move from one
/// demo to the next without going back to an index. The demo on screen is lit, and a screen reader hears it is the
/// current page.
/// </summary>
/// <remarks>
/// Master and detail (Microsoft's list/details pattern): the rail is the list the frame keeps beside the stage, its
/// links ordinary addresses, so a demo stays a page of its own that a search or a link opens directly.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/windows/apps/design/controls/list-details">List/details (Microsoft)</seealso>
public partial class DemoRail
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

    /// <summary>A group's heading in the site's words.</summary>
    /// <remarks>A switch on the group, one arm each (ADR 0051).</remarks>
    private string NameOf(DemoGroup group) => group switch
    {
        DemoGroup.ForYourBusiness => Text.ForYourBusiness,
        DemoGroup.AgentsAtWork => Text.AgentsAtWork,
        _ => group.Wire(),
    };

    /// <summary>A cover's classes: its colour, and lit when it's on screen.</summary>
    private string ClassesOf(ShowroomDemo demo) =>
        $"demo-cover accent-{demo.Accent.Wire()}{(demo == Current ? " is-current" : "")}";

    /// <summary>Marks the demo on screen as the current page; no mark on the others.</summary>
    private string? CurrentMark(ShowroomDemo demo) => demo == Current ? "page" : null;
}
