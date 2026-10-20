using LouisAgent.Puls.Search;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Puls.Components.Chat.Search;

public partial class ChatSearch
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter, EditorRequired] public List<ChatModel> Chats { get; set; } = [];

    private string _query = "";
    private int _selected;
    private bool _handled;
    private bool _wasOpen;

    /// <summary>The chats that match, newest first, each with the line that matched (null when the title did).</summary>
    private List<(ChatModel Chat, string? Snippet)> Results => ChatFinder.Find(Chats, _query);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Open && !_wasOpen) await JS.InvokeVoidAsync("pulsUi.focus", "#search-input");
        _wasOpen = Open;
    }

    protected override void OnParametersSet()
    {
        if (Open && !_wasOpen) { _query = ""; _selected = 0; }
    }

    private void Reset() => _selected = 0;

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

    private async Task Go(ChatModel chat)
    {
        await Close();
        Nav.NavigateTo($"gesels/{chat.SessionId}");
    }

    private Task Close() => OpenChanged.InvokeAsync(false);
}
