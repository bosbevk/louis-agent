using LouisAgent.Web.Admin.Explorer.Text;
using LouisAgent.Web.Admin.Explorer.Tree;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Evals.Explorer.Overview;

/// <summary>What each conversation cost in every mode, as bars and figures, with its replay and its way in.</summary>
public partial class CostBars
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The currency to show money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>Someone wants the chats side by side on a conversation.</summary>
    [Parameter]
    public EventCallback<string> OnReplay { get; set; }

    /// <summary>Someone opened a conversation's turns.</summary>
    [Parameter]
    public EventCallback<string> OnOpenConversation { get; set; }

    /// <summary>A cost in the chosen currency, or a dash.</summary>
    private string Money(decimal? usd) => ExplorerText.Money(usd, Currency);
}
