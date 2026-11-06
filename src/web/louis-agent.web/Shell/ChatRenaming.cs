using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;

namespace LouisAgent.Web.Shell;

/// <summary>Renaming a chat, in the sidebar or the top bar: which chat, where the field is, and what's typed.</summary>
/// <remarks>
/// Presentation Model (Fowler): the state and rules of the rename live in a plain class, and the components only draw
/// it. The sidebar and the top bar share one rename, so neither can own it, and a plain class can be tested without
/// rendering a page. It's the same shape as <see cref="ChatRunner"/>.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Presentation Model (Fowler)</seealso>
public sealed class ChatRenaming(ChatStore store)
{
    /// <summary>The longest name a chat keeps.</summary>
    private const int MostCharacters = 80;

    private bool _wantsFocus;

    /// <summary>The chat being renamed; null when none is.</summary>
    public string? SessionId { get; private set; }

    /// <summary>The field is in the top bar, not the sidebar.</summary>
    public bool InHeader { get; private set; }

    /// <summary>What's typed in the field.</summary>
    public string Draft { get; set; } = "";

    /// <summary>Opens the field on this chat.</summary>
    public void Start(Chat chat, bool inHeader)
    {
        SessionId = chat.SessionId;
        InHeader = inHeader;
        Draft = chat.Title;
        _wantsFocus = true;
    }

    /// <summary>The page should put the cursor in the field now; it says so once for each start.</summary>
    public bool TakeFocus()
    {
        bool wanted = _wantsFocus;
        _wantsFocus = false;
        return wanted;
    }

    /// <summary>Enter keeps the new name, and Escape drops it.</summary>
    public async Task KeyAsync(string key, Chat chat)
    {
        if (key == "Enter") await FinishAsync(chat);
        else if (key == "Escape") SessionId = null;
    }

    /// <summary>Keeps the new name and closes the field.</summary>
    public async Task FinishAsync(Chat chat)
    {
        if (SessionId != chat.SessionId) return;
        SessionId = null;
        string title = Draft.Trim();
        if (title.Length == 0 || title == chat.Title) return;
        chat.Title = title.Length > MostCharacters ? title[..MostCharacters] : title;
        await store.SaveAsync();
    }
}
