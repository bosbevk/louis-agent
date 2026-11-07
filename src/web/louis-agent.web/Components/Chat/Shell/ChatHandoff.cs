using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Chat.Shell;

/// <summary>
/// How a Review or Ask button on an admin screen reaches a chat: it opens the site's chat with the words as a draft.
/// It can't send them, so the person always reads the prompt and sends it themselves.
/// </summary>
/// <remarks>
/// The screen and the chat are on different pages and never exist together, so one can't call the other. This small
/// object outlives both for the moment between them. The words stay in memory rather than in the address, as a prompt
/// can be long and shouldn't end up in the browser's history.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public sealed class ChatHandoff(string chatAddress)
{
    private string? _draft;

    /// <summary>Keeps the words and goes to the site's chat.</summary>
    public void OpenChatWith(string draft, NavigationManager navigation)
    {
        _draft = draft;
        navigation.NavigateTo(chatAddress);
    }

    /// <summary>The waiting draft, once: coming back to the chat later doesn't fill the box again.</summary>
    public string? Take()
    {
        var draft = _draft;
        _draft = null;
        return draft;
    }
}
