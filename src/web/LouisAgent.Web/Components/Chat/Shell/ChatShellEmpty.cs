using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Shell;

/// <summary>What a site's empty chat is given: the message box to place, and how to start a turn or a draft.</summary>
public sealed record ChatShellEmpty(
    RenderFragment MessageBox,
    bool Streaming,
    EventCallback<string> OnDraft,
    EventCallback<string> OnSuggest);
