using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Chat.Sidebar;

/// <summary>One chat in the sidebar: its link or the rename field, and its menu.</summary>
public partial class ChatListItem
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter, EditorRequired] public ChatModel Chat { get; set; } = default!;

    /// <summary>Where this site keeps its chats, such as <c>chat</c> for <c>chat/{id}</c>.</summary>
    [Parameter] public string ChatPath { get; set; } = "chat";

    /// <summary>The chat the page shows.</summary>
    [Parameter] public bool Active { get; set; }

    /// <summary>This chat is being renamed here (not in the top bar): the field replaces the link.</summary>
    [Parameter] public bool Renaming { get; set; }
    [Parameter] public string RenameDraft { get; set; } = "";
    [Parameter] public EventCallback<string> RenameDraftChanged { get; set; }
    [Parameter] public EventCallback<(KeyboardEventArgs Key, ChatModel Chat)> OnRenameKey { get; set; }
    [Parameter] public EventCallback<ChatModel> OnRenameBlur { get; set; }

    [Parameter] public EventCallback<ChatModel> OnFavourite { get; set; }
    [Parameter] public EventCallback<(ChatModel Chat, bool InHeader)> OnRename { get; set; }
    [Parameter] public EventCallback<ChatModel> OnArchive { get; set; }
    [Parameter] public EventCallback<ChatModel> OnDelete { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }
}
