using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Parts;

/// <summary>The replay button on a conversation or a turn, and the one that goes a level deeper.</summary>
public partial class RowActions
{
    /// <summary>The conversation's task.</summary>
    [Parameter, EditorRequired]
    public string TaskId { get; set; } = "";

    /// <summary>The turn's number, on a turn; none on a conversation.</summary>
    [Parameter]
    public int? Turn { get; set; }

    /// <summary>Someone wants the chats side by side on this conversation.</summary>
    [Parameter]
    public EventCallback<string> OnReplay { get; set; }

    /// <summary>Someone wants this conversation's turns.</summary>
    [Parameter]
    public EventCallback<string> OnTurns { get; set; }

    /// <summary>Someone wants this turn's requests.</summary>
    [Parameter]
    public EventCallback<int> OnRequests { get; set; }
}
