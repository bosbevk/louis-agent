using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Evals.Components.EvalsPage.Details;

/// <summary>
/// A check's manual in a dialog over the page. It takes the focus when it opens, so Esc reaches it and a screen
/// reader starts there; Esc, the × or a click outside closes it.
/// </summary>
public partial class ManualDialog : IAsyncDisposable
{
    private JsModule? _dialogs;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The framework's dialogs module, which puts the focus where Blazor just drew (CL31).</summary>
    private JsModule Dialogs => _dialogs ??= new JsModule(JS, FrameworkScripts.Dialogs);

    /// <summary>The manual to show.</summary>
    [Parameter, EditorRequired]
    public ManualReply Manual { get; set; } = null!;

    /// <summary>The dialog was closed.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Puts the focus in the dialog the first time it shows.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        // The module may not load (a test, a page still loading): the dialog works without the focus.
        try
        {
            await Dialogs.InvokeVoidAsync("focus", "#manual-dialog");
        }
        catch (JSException)
        {
        }
    }

    /// <summary>Lets the dialogs module go, if it was ever loaded.</summary>
    public ValueTask DisposeAsync() => _dialogs?.DisposeAsync() ?? ValueTask.CompletedTask;

    /// <summary>Esc closes the dialog.</summary>
    private Task KeyAsync(KeyboardEventArgs e) => e.Key == "Escape" ? OnClose.InvokeAsync() : Task.CompletedTask;
}
