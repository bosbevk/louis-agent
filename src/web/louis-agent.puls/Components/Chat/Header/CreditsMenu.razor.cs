using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Header;

public partial class CreditsMenu
{
    /// <summary>What the shown chat cost, specialists included; "—" without a chat.</summary>
    [Parameter] public string ChatCost { get; set; } = "—";

    /// <summary>What this month's chats cost, and how many there were.</summary>
    [Parameter] public string MonthCost { get; set; } = "";
    [Parameter] public int MonthChats { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
