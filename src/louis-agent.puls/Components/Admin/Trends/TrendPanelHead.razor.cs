using LouisAgent.Puls.Admin.Trends;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Trends;

/// <summary>
/// The top line of one trend chart: what it measures, each mode's value at the run you're reading with how it moved
/// since the run before, which run that is, and the scale's ends. On a small card it's just the title.
/// </summary>
public partial class TrendPanelHead
{
    /// <summary>What the chart measures, so its note on hover explains the right thing.</summary>
    [Parameter, EditorRequired]
    public string Topic { get; set; } = "";

    /// <summary>The chart's name as the page shows it.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>A check card's small chart, which shows only its title.</summary>
    [Parameter]
    public bool Small { get; set; }

    /// <summary>Each mode's value at the run being read, with its change.</summary>
    [Parameter]
    public IReadOnlyList<TrendValue> Values { get; set; } = [];

    /// <summary>The run a hover is reading in this panel; null when the mouse is elsewhere.</summary>
    [Parameter]
    public TrendRun? Hovered { get; set; }

    /// <summary>The scale's ends ("0 – 75%"), or why there is none.</summary>
    [Parameter]
    public string Top { get; set; } = "";
}
