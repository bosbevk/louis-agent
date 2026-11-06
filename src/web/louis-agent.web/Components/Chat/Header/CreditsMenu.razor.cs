using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Header;

/// <summary>What this chat and this month cost, from the chats in this browser.</summary>
public partial class CreditsMenu
{
    [Inject] private ChatText Text { get; set; } = null!;

    /// <summary>What the shown chat cost, specialists included; "—" without a chat.</summary>
    [Parameter] public string ChatCost { get; set; } = "—";

    /// <summary>What this month's chats cost, and how many there were.</summary>
    [Parameter] public string MonthCost { get; set; } = "";
    [Parameter] public int MonthChats { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
