using LouisAgent.Web.Evals.Admin.Explorer.Sites;
using LouisAgent.Web.Evals.Admin.Explorer.Tree;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Explorer.Overview.Charts;

/// <summary>What each mode used and got for it: the cache, its tools, the sites it read, what an answer cost.</summary>
public partial class UseCharts
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>How this site sorts the websites its agent reads.</summary>
    [Inject] private SiteKinds Kinds { get; set; } = null!;

    /// <summary>The sites a mode's turns read, sorted by kind.</summary>
    private SiteCounts Sites(ModeNode m) => SiteCounts.Of(m.Turns.SelectMany(t => t.Hosts), Kinds);
}
