using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Widget.Door;

/// <summary>
/// A button in a site's header that opens its agent's chat with nothing asked (F164.2), "Ask Orbi" on the framework's
/// site, and Ctrl+I or Cmd+I from anywhere on the page, as a docs site's assistant opens.
/// </summary>
/// <remarks>
/// It opens the chat through the door, as <c>AskButton</c> asks through it, so it needs no reference to the widget
/// (Observer). Its keys are its own module's (ADR 0093), which presses the button, so a key and a press are one path,
/// as the chat shell's Ctrl+K presses its search button.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public partial class OpenAgentButton : IAsyncDisposable
{
    /// <summary>The button's words.</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = "";

    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The button, which its keys press.</summary>
    private ElementReference _button;

    /// <summary>The button's module, which holds its keys.</summary>
    private JsModule? _module;

    /// <summary>Opens the chat through the door, when the site has one.</summary>
    private void OpenChat() => Services.GetService<GuideDoor>()?.Open();

    /// <summary>Hands the button to its module once it is drawn, so its keys press it.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _module = new JsModule(JS, FrameworkScripts.OpenAgentButton);
        await _module.InvokeVoidAsync("attach", _button);
    }

    /// <summary>Takes its keys down, and lets the module go with the button.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("detach");
        await _module.DisposeAsync();
    }
}
