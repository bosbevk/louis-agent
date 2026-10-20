using LouisAgent.Web.Git;
using Microsoft.AspNetCore.Components;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Web.Components.Workspace;

public partial class WorkspaceSidebar
{
    private enum SidebarTab { Chats, Files, Changes, Branches }

    private SidebarTab _tab = SidebarTab.Chats;

    /// <summary>The saved chats, newest first; the list is the store's, so a chat added during a turn shows at once.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ChatModel> Chats { get; set; } = default!;

    /// <summary>The chat the page shows, to mark it in the list.</summary>
    [Parameter] public string? SessionId { get; set; }

    // The badges on the Changes and Branches tabs.
    [Parameter] public int ChangeCount { get; set; }
    [Parameter] public int UnmergedCount { get; set; }

    /// <summary>The saved API key, for the settings field.</summary>
    [Parameter] public string? ApiKeyDraft { get; set; }

    /// <summary>A file, diff, branch or commit is open instead of the chat, so no chat is marked.</summary>
    [Parameter] public bool ViewerOpen { get; set; }

    // What the viewers show, to mark its row in the files, changes and branches panels.
    [Parameter] public string? OpenFilePath { get; set; }
    [Parameter] public string? OpenDiffKey { get; set; }
    [Parameter] public string? OpenHistoryKey { get; set; }

    [Parameter] public EventCallback OnNewChat { get; set; }
    [Parameter] public EventCallback<string> OnOpenFile { get; set; }
    [Parameter] public EventCallback<(GitFile File, bool Staged)> OnOpenDiff { get; set; }
    [Parameter] public EventCallback OnReviewAll { get; set; }
    [Parameter] public EventCallback<string> OnOpenBranch { get; set; }
    [Parameter] public EventCallback<string> OnOpenCommit { get; set; }

    /// <summary>A chat's link was followed: the page closes what a viewer shows.</summary>
    [Parameter] public EventCallback OnOpenChat { get; set; }
    [Parameter] public EventCallback<ChatModel> OnDeleteChat { get; set; }
    [Parameter] public EventCallback<string?> OnSaveApiKey { get; set; }
}
