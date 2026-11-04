using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Charts;

/// <summary>A ring of parts (F18 step 6a.3b): the page hands it the parts, their colours and titles (one each), the total for its middle and the unit under it.</summary>
public partial class Ring
{
    [Parameter, EditorRequired] public IReadOnlyList<double> Parts { get; set; } = [];

    /// <summary>One colour per part, so a part of 0 keeps its own colour.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Colours { get; set; } = [];

    /// <summary>One title per part: the arc's hover and the ring's label for a screen reader.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Titles { get; set; } = [];

    [Parameter, EditorRequired] public string Centre { get; set; } = "";

    [Parameter] public string Unit { get; set; } = "";
}
