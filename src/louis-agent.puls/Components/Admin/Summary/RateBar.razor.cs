using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Summary;

/// <summary>A rate as a bar (F18 step 6a.3c).</summary>
public partial class RateBar
{
    /// <summary>The rate, 0 to 1; null draws nothing.</summary>
    [Parameter]
    public double? Rate { get; set; }

    /// <summary>Orange, for pass^k.</summary>
    [Parameter]
    public bool Hat { get; set; }
}
