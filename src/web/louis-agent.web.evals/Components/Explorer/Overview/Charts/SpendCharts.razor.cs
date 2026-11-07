using LouisAgent.Web.Evals.Admin.Explorer.Tree;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Explorer.Overview.Charts;

/// <summary>What each mode spent: its cost, its tokens and the model's time.</summary>
public partial class SpendCharts
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }
}
