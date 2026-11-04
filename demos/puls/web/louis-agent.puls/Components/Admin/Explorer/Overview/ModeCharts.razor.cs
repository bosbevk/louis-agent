using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Overview;

/// <summary>The fold of charts that compare the modes, each chart a donut per mode.</summary>
public partial class ModeCharts
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }
}
