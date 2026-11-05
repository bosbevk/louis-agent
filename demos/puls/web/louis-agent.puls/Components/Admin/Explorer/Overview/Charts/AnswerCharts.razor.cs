using LouisAgent.Puls.Admin.Explorer.Tree;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Overview.Charts;

/// <summary>How well each mode answered: its routing, and where the check lists ingredients, its accuracy.</summary>
public partial class AnswerCharts
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;
}
