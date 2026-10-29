using LouisAgent.Api.Contracts.Admin.Checks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.Checks;

/// <summary>
/// A check's manual in a dialog over the page. It takes the focus when it opens, so Esc reaches it and a screen
/// reader starts there; Esc, the × or a click outside closes it.
/// </summary>
public partial class ManualDialog
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

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
        // The page's script may not be there (a test, a page still loading): the dialog works without the focus.
        try
        {
            await JS.InvokeVoidAsync("pulsUi.focus", "#manual-dialog");
        }
        catch (JSException)
        {
        }
    }

    /// <summary>Esc closes the dialog.</summary>
    private Task KeyAsync(KeyboardEventArgs e) => e.Key == "Escape" ? OnClose.InvokeAsync() : Task.CompletedTask;
}
