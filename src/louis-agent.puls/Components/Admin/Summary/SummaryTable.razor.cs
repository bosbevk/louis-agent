using LouisAgent.Puls.Admin.Summary;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Summary;

/// <summary>One mode's roll-up as a table (F18 step 5c.3).</summary>
public partial class SummaryTable
{
    [Parameter, EditorRequired] public ModeSlices Mode { get; set; } = null!;

    /// <summary>The run the slices are of: its explorer opens a task's conversation.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    [Parameter] public CurrencyChoice? Currency { get; set; }
}
