using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Chat.Sidebar;

/// <summary>A chat's actions: favourite, share, rename, archive, delete.</summary>
public partial class ChatMenu
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter, EditorRequired] public ChatModel Chat { get; set; } = default!;

    /// <summary>True in the top bar's menu: the rename field then opens there, not in the sidebar.</summary>
    [Parameter] public bool InHeader { get; set; }

    [Parameter] public EventCallback<ChatModel> OnFavourite { get; set; }
    [Parameter] public EventCallback<(ChatModel Chat, bool InHeader)> OnRename { get; set; }
    [Parameter] public EventCallback<ChatModel> OnArchive { get; set; }
    [Parameter] public EventCallback<ChatModel> OnDelete { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
