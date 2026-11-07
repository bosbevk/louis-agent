using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Chat.Sidebar;

/// <summary>The chat's sidebar: the brand, a new chat, search, the site's views, the chats, and the settings.</summary>
public partial class ChatSidebar
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter, EditorRequired] public IReadOnlyList<ChatModel> Chats { get; set; } = default!;

    /// <summary>Where this site keeps its chats, such as <c>chat</c> for <c>chat/{id}</c>.</summary>
    [Parameter] public string ChatPath { get; set; } = "chat";

    /// <summary>The site's logo and anything beside it.</summary>
    [Parameter] public RenderFragment? Brand { get; set; }

    /// <summary>What the logo says when you hover over it.</summary>
    [Parameter] public string? BrandTitle { get; set; }

    /// <summary>The site's own links under search, such as its other views.</summary>
    [Parameter] public RenderFragment? Views { get; set; }

    /// <summary>Who's signed in, above the settings.</summary>
    [Parameter] public RenderFragment? Profile { get; set; }

    /// <summary>The site's own settings, with the frame's.</summary>
    [Parameter] public RenderFragment? Settings { get; set; }

    /// <summary>The parts this site turns off.</summary>
    [Parameter] public ChatFeatures Features { get; set; } = new();

    /// <summary>The chat the page shows, by its session; null for none.</summary>
    [Parameter] public string? SessionId { get; set; }

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
