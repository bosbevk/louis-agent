using LouisAgent.Puls.Admin.Explorer.Tree;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Head;

/// <summary>Shows how deep you've drilled into the run, and takes you back up a level.</summary>
public partial class ExplorerCrumbs
{
    /// <summary>How deep: 0 the run, 1 the conversations, 2 one conversation, 3 one turn, 4 one request.</summary>
    [Parameter, EditorRequired]
    public int Level { get; set; }

    /// <summary>The open conversation's task.</summary>
    [Parameter]
    public string? OpenTask { get; set; }

    /// <summary>The open turn's number.</summary>
    [Parameter]
    public int? Turn { get; set; }

    /// <summary>The open request.</summary>
    [Parameter]
    public RequestNode? Request { get; set; }

    /// <summary>The name of the mode the open request is from.</summary>
    [Parameter]
    public string? RequestMode { get; set; }

    /// <summary>Someone went back to a level.</summary>
    [Parameter]
    public EventCallback<int> OnGo { get; set; }

    /// <summary>Someone opened the chats side by side, on the open conversation if there is one.</summary>
    [Parameter]
    public EventCallback<string?> OnChats { get; set; }
}
