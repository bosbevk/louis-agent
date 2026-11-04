using LouisAgent.Puls.Content;
using Microsoft.AspNetCore.Components;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Puls.Components.Chat.Panels;

public partial class ChatViews
{
    /// <summary>Which view the shell shows: <c>verken</c> or <c>studio</c>; anything else draws nothing.</summary>
    [Parameter] public string View { get; set; } = "chat";

    /// <summary>Studio's recent chats.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ChatModel> Recent { get; set; } = [];
    [Parameter] public EventCallback<Template> OnStart { get; set; }
    [Parameter] public EventCallback OnNewProject { get; set; }
}
