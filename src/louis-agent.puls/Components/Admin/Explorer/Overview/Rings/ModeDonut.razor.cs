using LouisAgent.Puls.Admin.Explorer.Rings;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Overview.Rings;

/// <summary>Draws one mode's donut in a "per mode" chart, named and coloured for the mode.</summary>
public partial class ModeDonut
{
    /// <summary>The mode's place among the modes, for its colour.</summary>
    [Parameter, EditorRequired]
    public int I { get; set; }

    /// <summary>Who the mode is.</summary>
    [Parameter, EditorRequired]
    public string Who { get; set; } = "";

    /// <summary>What the donut draws.</summary>
    [Parameter, EditorRequired]
    public RingFigure Figure { get; set; } = null!;

    /// <summary>A note under the mode's name, when the chart has one.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
