using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Chat.Header;

/// <summary>The chat's top bar: its title and menu, then the work panel, credits, language and profile.</summary>
public partial class ChatHeader
{
    [Inject] private ChatText Text { get; set; } = null!;

    /// <summary>The parts this site turns off.</summary>
    [Parameter] public ChatFeatures Features { get; set; } = new();

    /// <summary>The title of a view a site shows instead of the chat; with none, the chat's own shows.</summary>
    [Parameter] public string? ViewTitle { get; set; }

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

    // The credits.
    [Parameter] public string ChatCost { get; set; } = "—";
    [Parameter] public string MonthCost { get; set; } = "";
    [Parameter] public int MonthChats { get; set; }

    // The language.
    [Parameter] public string Language { get; set; } = "en";
    [Parameter] public IReadOnlyList<string> Languages { get; set; } = ["en"];
    [Parameter] public EventCallback<string> OnChooseLanguage { get; set; }
}
