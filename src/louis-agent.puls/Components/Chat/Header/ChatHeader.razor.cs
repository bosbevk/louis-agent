using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ChatModel = LouisAgent.Web.Shared.Chat.Chat;

namespace LouisAgent.Puls.Components.Chat.Header;

public partial class ChatHeader
{
    /// <summary>Which view the shell shows: the chat, <c>verken</c> or <c>studio</c>.</summary>
    [Parameter] public string View { get; set; } = "chat";

    /// <summary>The chat shown; null for a new one.</summary>
    [Parameter] public ChatModel? Chat { get; set; }

    // Renaming: the session being renamed, whether the field is here, and its text (the page keeps them: the sidebar renames too).
    [Parameter] public string? Renaming { get; set; }
    [Parameter] public bool RenameInHeader { get; set; }
    [Parameter] public string RenameDraft { get; set; } = "";
    [Parameter] public EventCallback<string> RenameDraftChanged { get; set; }
    [Parameter] public EventCallback<(KeyboardEventArgs Key, ChatModel Chat)> OnRenameKey { get; set; }
    [Parameter] public EventCallback<ChatModel> OnRenameBlur { get; set; }

    // The chat's menu.
    [Parameter] public EventCallback<ChatModel> OnFavourite { get; set; }
    [Parameter] public EventCallback<(ChatModel Chat, bool InHeader)> OnRename { get; set; }
    [Parameter] public EventCallback<ChatModel> OnArchive { get; set; }
    [Parameter] public EventCallback<ChatModel> OnDelete { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }

    // Krediete.
    [Parameter] public string ChatCost { get; set; } = "—";
    [Parameter] public string MonthCost { get; set; } = "";
    [Parameter] public int MonthChats { get; set; }

    // The language.
    [Parameter] public string Language { get; set; } = "af";
    [Parameter] public IReadOnlyList<string> Languages { get; set; } = ["af"];
    [Parameter] public EventCallback<string> OnChooseLanguage { get; set; }
}
