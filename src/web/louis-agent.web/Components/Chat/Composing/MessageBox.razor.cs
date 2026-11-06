using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Composing;

/// <summary>The message box: the composer with its toolbar.</summary>
public partial class MessageBox
{
    [Inject] private ChatText Text { get; set; } = null!;

    /// <summary>A turn's life: the draft and its attachments flow down from it, and sending goes back to it.</summary>
    [Parameter, EditorRequired] public ChatRunner Runner { get; set; } = default!;

    /// <summary>The instance's model, prettified; null until known.</summary>
    [Parameter] public string? Model { get; set; }
    [Parameter] public EventCallback<string> OnDraft { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
