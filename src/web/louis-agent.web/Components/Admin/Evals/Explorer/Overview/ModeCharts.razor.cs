using LouisAgent.Web.Admin.Explorer.Tree;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Evals.Explorer.Overview;

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
