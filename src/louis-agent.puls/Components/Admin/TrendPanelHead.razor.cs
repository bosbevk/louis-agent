using LouisAgent.Puls.Admin.Trends;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin;

public partial class TrendPanelHead
{
    [Parameter, EditorRequired] public string Topic { get; set; } = "";
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public bool Small { get; set; }
    [Parameter] public IReadOnlyList<TrendValue> Values { get; set; } = [];

    /// <summary>The run a hover is reading in this panel; null when the mouse is elsewhere.</summary>
    [Parameter] public TrendRun? Hovered { get; set; }

    /// <summary>The scale's ends ("0 – 75%"), or why there is none.</summary>
    [Parameter] public string Top { get; set; } = "";
}
