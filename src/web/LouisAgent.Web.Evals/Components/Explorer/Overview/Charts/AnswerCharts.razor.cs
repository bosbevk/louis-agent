using LouisAgent.Web.Evals.Admin.Explorer.Tree;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Explorer.Overview.Charts;

/// <summary>How well each mode answered: its routing, and where the check lists ingredients, its accuracy.</summary>
public partial class AnswerCharts
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;
}
