using LouisAgent.Puls.Admin.Explorer.Rings;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Overview.Charts;

/// <summary>What each mode used and got for it: the cache, its tools, the sites it read, what an answer cost.</summary>
public partial class UseCharts
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>The sites a mode's turns read, sorted by kind.</summary>
    private static SiteCounts Sites(ModeNode m) => SiteCounts.Of(m.Turns.SelectMany(t => t.Hosts));
}
