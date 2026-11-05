using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Header;

public partial class ProfileMenu
{
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
