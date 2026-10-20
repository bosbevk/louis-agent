using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Puls.Components.Chat.Sidebar;

public partial class ChatSidebar
{
    [Parameter, EditorRequired] public IReadOnlyList<ChatModel> Chats { get; set; } = default!;

    /// <summary>The chat the page shows, by its session; null for none.</summary>
    [Parameter] public string? SessionId { get; set; }

    /// <summary>Which view the shell shows: the chat, <c>verken</c> or <c>studio</c>.</summary>
    [Parameter] public string View { get; set; } = "chat";

    [Parameter] public EventCallback OnNewChat { get; set; }
    [Parameter] public EventCallback OnSearch { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }

    // Renaming: the session being renamed, where the field is, and its text (the page keeps them: the top bar renames too).
    [Parameter] public string? Renaming { get; set; }
    [Parameter] public bool RenameInHeader { get; set; }
    [Parameter] public string RenameDraft { get; set; } = "";
    [Parameter] public EventCallback<string> RenameDraftChanged { get; set; }
    [Parameter] public EventCallback<(KeyboardEventArgs Key, ChatModel Chat)> OnRenameKey { get; set; }
    [Parameter] public EventCallback<ChatModel> OnRenameBlur { get; set; }

    // A chat's menu.
    [Parameter] public EventCallback<ChatModel> OnFavourite { get; set; }
    [Parameter] public EventCallback<(ChatModel Chat, bool InHeader)> OnRename { get; set; }
    [Parameter] public EventCallback<ChatModel> OnArchive { get; set; }
    [Parameter] public EventCallback<ChatModel> OnDelete { get; set; }

    // The settings.
    [Parameter] public string? Location { get; set; }
    [Parameter] public bool Locating { get; set; }
    [Parameter] public EventCallback<string?> OnSaveLocation { get; set; }
    [Parameter] public EventCallback OnLocate { get; set; }
    [Parameter] public bool Motion { get; set; }
    [Parameter] public bool ReducedMotion { get; set; }
    [Parameter] public EventCallback<bool> OnSaveMotion { get; set; }
    [Parameter] public string? ApiKeyDraft { get; set; }
    [Parameter] public EventCallback<string?> OnSaveApiKey { get; set; }
}
