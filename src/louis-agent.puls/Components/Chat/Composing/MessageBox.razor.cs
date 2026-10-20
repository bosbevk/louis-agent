using LouisAgent.Web.Shared.Chats;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Chat.Composing;

public partial class MessageBox
{
    /// <summary>A turn's life (W2): the draft and its attachments flow down from it and sending goes back to it.</summary>
    [Parameter, EditorRequired] public ChatRunner Runner { get; set; } = default!;

    /// <summary>The instance's model, prettified; null until known.</summary>
    [Parameter] public string? Model { get; set; }
    [Parameter] public EventCallback<string> OnDraft { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
