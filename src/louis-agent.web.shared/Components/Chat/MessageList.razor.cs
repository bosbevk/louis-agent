using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Web.Shared.Components.Chat;

public partial class MessageList
{
    [Parameter] public ChatModel? Chat { get; set; }
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>What to show while the chat has no turn (the app's welcome).</summary>
    [Parameter] public RenderFragment? Empty { get; set; }

    /// <summary>What to show instead of the turns, when the app's view isn't the chat.</summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>Classes after <c>messages</c>; <c>hidden</c> while the code-review app shows a file or a diff instead.</summary>
    [Parameter] public string? Class { get; set; }

    [Parameter] public RenderFragment<Turn>? UserActions { get; set; }
    [Parameter] public RenderFragment<Turn>? AnswerActions { get; set; }

    private ElementReference _messages;
    private ChatModel? _shown;
    private bool _wasStreaming;
    private string? _lastClass;
    private bool _scrollToEnd;

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Chat, _shown)) _scrollToEnd = true;
        if (Streaming && !_wasStreaming) _scrollToEnd = true;
        if (_lastClass == "hidden" && Class != "hidden") _scrollToEnd = true;
        _shown = Chat;
        _wasStreaming = Streaming;
        _lastClass = Class;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Streaming || _scrollToEnd)
        {
            await JS.InvokeVoidAsync("louisChat.scrollToBottom", _messages, _scrollToEnd);
            _scrollToEnd = false;
        }
    }
}
