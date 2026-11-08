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
    private ElementReference _input;
    private JsModule? _keys;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The box's own module, loaded on first use (CL31).</summary>
    private JsModule Keys => _keys ??= new JsModule(JS, FrameworkScripts.Composer);

    [Parameter] public string Draft { get; set; } = "";
    [Parameter] public EventCallback<string> DraftChanged { get; set; }
    [Parameter, EditorRequired] public List<Attachment> Attachments { get; set; } = default!;
    [Parameter] public bool Streaming { get; set; }
    [Parameter] public bool CanSend { get; set; }

    /// <summary>True once the chat ended (its session is gone): the box takes no more text.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    [Parameter] public string? Placeholder { get; set; }

    [Parameter] public EventCallback OnSend { get; set; }
    [Parameter] public EventCallback OnStop { get; set; }

    /// <summary>The class of the box around the text, which each site styles.</summary>
    [Parameter]
    public string BoxClass { get; set; } = "input-row";

    /// <summary>What a site puts before the text, such as an attach button and a prompt sign.</summary>
    [Parameter]
    public RenderFragment? BeforeInput { get; set; }

    /// <summary>A site's toolbar after the text; the Send and Stop buttons then close it.</summary>
    [Parameter]
    public RenderFragment? Tools { get; set; }

    [Parameter] public string SendLabel { get; set; } = "Send";
    [Parameter] public string StopLabel { get; set; } = "Stop";
    [Parameter] public string? SendTitle { get; set; }
    [Parameter] public string? StopTitle { get; set; }

    /// <summary>Hangs the key rule on the box once it exists in the page.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await Keys.InvokeVoidAsync("attach", _input);
    }

    /// <summary>Lets the module go with the box.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_keys is not null) await _keys.DisposeAsync();
    }
}
