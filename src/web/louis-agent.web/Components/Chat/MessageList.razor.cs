using LouisAgent.Web.Chats;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Components.Chat;

/// <summary>A chat's turns, or what the site shows instead, scrolled to the end as the answer grows.</summary>
public partial class MessageList : IAsyncDisposable
{
    private JsModule? _list;

    /// <summary>The list's own module, beside it (CL31): the scrolling.</summary>
    private JsModule List => _list ??= new JsModule(JS, FrameworkScripts.MessageList);

    [Parameter] public ChatModel? Chat { get; set; }
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>What to show while the chat has no turn (the app's welcome).</summary>
    [Parameter]
    public RenderFragment? Empty { get; set; }

    /// <summary>What to show instead of the turns, when the app's view isn't the chat.</summary>
    [Parameter]
    public RenderFragment? Content { get; set; }

    /// <summary>Classes after <c>messages</c>; <c>hidden</c> while a site shows something else instead.</summary>
    [Parameter]
    public string? Class { get; set; }

    [Parameter] public RenderFragment<Turn>? UserActions { get; set; }
    [Parameter] public RenderFragment<Turn>? AnswerActions { get; set; }

    /// <summary>Whether the answers show what they thought and the tools they called; off, their words alone.</summary>
    [Parameter]
    public bool ShowWork { get; set; } = true;

    /// <summary>Whether a finished answer shows what it used and cost; off on a business's website (F127.2).</summary>
    [Parameter]
    public bool ShowUsage { get; set; } = true;

    /// <summary>
    /// What a click on a card the agent offered does with its label (F127.4.1): the chat that owns a runner sends it;
    /// a recording gives none, and its cards wait.
    /// </summary>
    [Parameter]
    public EventCallback<string> Chosen { get; set; }

    private ElementReference _messages;
    private ChatModel? _shown;
    private bool _wasStreaming;
    private string? _lastClass;
    private bool _scrollToEnd;
    private bool _streamEnded;

    /// <summary>Notes a new chat or a new turn, so the next draw knows to scroll.</summary>
    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Chat, _shown)) _scrollToEnd = true;
        if (Streaming && !_wasStreaming) _scrollToEnd = true;
        // The draw after the stream ends adds the cost line and takes the caret away (FX98): follow it to the end too.
        if (!Streaming && _wasStreaming) _streamEnded = true;
        if (_lastClass == "hidden" && Class != "hidden") _scrollToEnd = true;
        _shown = Chat;
        _wasStreaming = Streaming;
        _lastClass = Class;
    }

    /// <summary>Scrolls to the end through the list's module, when something new came in.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Streaming || _scrollToEnd || _streamEnded)
        {
            await List.InvokeVoidAsync("scrollToBottom", _messages, _scrollToEnd);
            _scrollToEnd = false;
            _streamEnded = false;
        }
    }

    /// <summary>The visitor's message after a turn, once they sent one: the answer to what that turn offered.</summary>
    private string? AnswerTo(Turn turn)
    {
        int at = Chat!.Turns.IndexOf(turn);
        return at + 1 < Chat.Turns.Count ? Chat.Turns[at + 1].UserText : null;
    }

    /// <summary>Lets the module go, if it was ever loaded.</summary>
    public ValueTask DisposeAsync() => _list?.DisposeAsync() ?? ValueTask.CompletedTask;
}
