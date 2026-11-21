using LouisAgent.Web.Chats;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Chat;

/// <summary>
/// The message box: the attachments, the text, and Send or Stop. It hangs its own key rule on the box when it first
/// draws (Enter sends, Shift+Enter breaks the line), so the rule reaches every box, in the chat shell or a site's
/// corner (FX70).
/// </summary>
public partial class Composer : IAsyncDisposable
{
    /// <summary>The text box on the page.</summary>
    private ElementReference _input;

    /// <summary>The box's module, once it is loaded.</summary>
    private JsModule? _keys;

    /// <summary>The browser, for the box's module.</summary>
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>The box's own module, loaded on first use (CL31).</summary>
    private JsModule Keys => _keys ??= new JsModule(JS, FrameworkScripts.Composer);

    /// <summary>What is typed so far.</summary>
    [Parameter]
    public string Draft { get; set; } = "";

    /// <summary>Says what is typed, as it changes.</summary>
    [Parameter]
    public EventCallback<string> DraftChanged { get; set; }

    /// <summary>The files attached to the message, shown as chips over the box.</summary>
    [Parameter, EditorRequired]
    public List<Attachment> Attachments { get; set; } = default!;

    /// <summary>Whether an answer is coming in: the box then shows Stop in place of Send.</summary>
    [Parameter]
    public bool Streaming { get; set; }

    /// <summary>Whether there is something to send.</summary>
    [Parameter]
    public bool CanSend { get; set; }

    /// <summary>True once the chat ended (its session is gone): the box takes no more text.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>What the empty box says.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Asks the page to send what is typed.</summary>
    [Parameter]
    public EventCallback OnSend { get; set; }

    /// <summary>Asks the page to stop the answer coming in.</summary>
    [Parameter]
    public EventCallback OnStop { get; set; }

    /// <summary>The class of the box around the text, which each site styles.</summary>
    [Parameter]
    public string BoxClass { get; set; } = "input-row";

    /// <summary>What a site puts before the text, such as an attach button and a prompt sign.</summary>
    [Parameter]
    public RenderFragment? BeforeInput { get; set; }

    /// <summary>A site's toolbar after the text; the Send and Stop buttons then close it.</summary>
    [Parameter]
    public RenderFragment? Tools { get; set; }

    /// <summary>The words on Send.</summary>
    [Parameter]
    public string SendLabel { get; set; } = "Send";

    /// <summary>The words on Stop.</summary>
    [Parameter]
    public string StopLabel { get; set; } = "Stop";

    /// <summary>Send's tip.</summary>
    [Parameter]
    public string? SendTitle { get; set; }

    /// <summary>Stop's tip.</summary>
    [Parameter]
    public string? StopTitle { get; set; }

    /// <summary>
    /// Puts the visitor in this box. A page with more than one box can't reach the right one by its id, which every
    /// box shares, so whoever holds this box asks it.
    /// </summary>
    public ValueTask FocusAsync() => _input.FocusAsync();

    /// <summary>Hangs the key rule on the box once it exists in the page.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await Keys.InvokeVoidAsync("attach", _input);
    }

    /// <summary>Lets the module go with the box.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_keys is not null) await _keys.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
