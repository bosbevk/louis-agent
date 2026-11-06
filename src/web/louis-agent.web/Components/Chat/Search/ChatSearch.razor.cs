using LouisAgent.Web.Search;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Chat.Search;

/// <summary>Searches the chats saved in this browser: their titles, your messages and the answers.</summary>
public partial class ChatSearch
{
    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter, EditorRequired] public List<ChatModel> Chats { get; set; } = [];

    /// <summary>Where this site keeps its chats, such as <c>chat</c> for <c>chat/{id}</c>.</summary>
    [Parameter] public string ChatPath { get; set; } = "chat";

    private string _query = "";
    private ElementReference _input;
    private int _selected;
    private bool _handled;
    private bool _wasOpen;

    /// <summary>The chats that match, newest first, each with the line that matched (null if the title did).</summary>
    private List<(ChatModel Chat, string? Snippet)> Results => ChatFinder.Find(Chats, _query);

    /// <summary>Puts the cursor in the search box when it opens.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Open && !_wasOpen) await _input.FocusAsync();
        _wasOpen = Open;
    }

    /// <summary>Starts clean each time it opens.</summary>
    protected override void OnParametersSet()
    {
        if (Open && !_wasOpen) { _query = ""; _selected = 0; }
    }

    /// <summary>A new query selects the first result again.</summary>
    private void Reset() => _selected = 0;

    /// <summary>The arrow keys move the selection, Enter opens it, and Escape closes the search.</summary>
    private async Task Key(KeyboardEventArgs e)
    {
        _handled = e.Key is "ArrowDown" or "ArrowUp" or "Enter";
        var results = Results;
        switch (e.Key)
        {
            case "Escape": await Close(); break;
            case "ArrowDown": _selected = Math.Min(_selected + 1, Math.Max(results.Count - 1, 0)); break;
            case "ArrowUp": _selected = Math.Max(_selected - 1, 0); break;
            case "Enter" when results.Count > 0: await Go(results[Math.Min(_selected, results.Count - 1)].Chat); break;
        }
    }

    /// <summary>Opens the chat and closes the search.</summary>
    private async Task Go(ChatModel chat)
    {
        await Close();
        Nav.NavigateTo($"{ChatPath}/{chat.SessionId}");
    }

    /// <summary>Closes the search.</summary>
    private Task Close() => OpenChanged.InvokeAsync(false);
}
