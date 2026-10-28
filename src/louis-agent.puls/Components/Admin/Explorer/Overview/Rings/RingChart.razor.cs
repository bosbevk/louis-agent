using LouisAgent.Puls.Admin.Explorer.Legends;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Overview.Rings;

/// <summary>Frames one "per mode" chart: its title and note, its legend, and the donuts it is given.</summary>
public partial class RingChart
{
    /// <summary>The measure, by its word, for the title's note.</summary>
    [Parameter, EditorRequired]
    public string Topic { get; set; } = "";

    /// <summary>The chart's title.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>The legend's entries; with none, the four kinds of token.</summary>
    [Parameter]
    public IReadOnlyList<LegendEntry>? Legend { get; set; }

    /// <summary>A donut per mode.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
