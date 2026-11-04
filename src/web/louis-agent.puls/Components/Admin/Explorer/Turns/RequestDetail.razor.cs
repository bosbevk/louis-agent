using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Turns;

/// <summary>Shows one model request in full, as the ledger kept it.</summary>
public partial class RequestDetail
{
    /// <summary>The request.</summary>
    [Parameter, EditorRequired]
    public RequestNode Request { get; set; } = null!;

    /// <summary>The mode it was made in.</summary>
    [Parameter, EditorRequired]
    public ModeNode Mode { get; set; } = null!;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>What it cost in the chosen currency, or a dash.</summary>
    private string Money => ExplorerText.Money(Request.Cost, Currency);

    /// <summary>Whose lane it ran in: Puls's, or the mode's specialists'.</summary>
    private string Lane => Request.Specialist ? ModeNames.Roles([Mode]) : "Puls";
}
