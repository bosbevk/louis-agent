using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Turns;

/// <summary>Under the user's message: copy it, or edit it.</summary>
public partial class UserActions
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter, EditorRequired] public Turn Turn { get; set; } = default!;
    [Parameter] public EventCallback<string> OnCopy { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }

    /// <summary>Whether what only says it's coming shows too.</summary>
    [Parameter] public bool Unfinished { get; set; } = true;
}
