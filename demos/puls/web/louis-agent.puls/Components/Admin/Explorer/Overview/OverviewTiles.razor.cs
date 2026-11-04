using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Overview;

/// <summary>
/// The run at a glance: a tile per mode with what it cost, used and scored. A tile opens the conversations.
/// </summary>
public partial class OverviewTiles
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Someone opened the conversations.</summary>
    [Parameter]
    public EventCallback OnOpen { get; set; }

    /// <summary>A cost in the chosen currency, or a dash.</summary>
    private string Money(decimal? usd) => ExplorerText.Money(usd, Currency);

    /// <summary>The mode's errors after its score, when it had any.</summary>
    private static string Errors(ModeNode m) => m.Errors > 0 ? $" · {m.Errors} errors" : "";
}
