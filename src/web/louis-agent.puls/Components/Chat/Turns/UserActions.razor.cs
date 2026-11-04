using LouisAgent.Web.Shared.Chats;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Turns;

public partial class UserActions
{
    [Parameter, EditorRequired] public Turn Turn { get; set; } = default!;
    [Parameter] public EventCallback<string> OnCopy { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
