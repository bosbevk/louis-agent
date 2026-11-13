using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Site.Components.Cv;

/// <summary>
/// The CV's "Save as PDF" button (F142). One page, two outputs: it asks the browser to print, and the browser's own
/// dialog saves a PDF. The site's print styles lay the page out on A4, so no PDF is ever built by us.
/// </summary>
/// <remarks>
/// The call to the browser is a script module beside the button, loaded when it is first pressed, as every script
/// here is (ADR 0093).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public partial class ResumePrint : IAsyncDisposable
{
    /// <summary>The button's own script module, served beside it.</summary>
    public const string Script = "./Components/Cv/ResumePrint.razor.js";

    private JsModule? _module;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Lets the module go, if it was ever loaded.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Opens the browser's print dialog.</summary>
    private async Task PrintAsync()
    {
        _module ??= new JsModule(JS, Script);
        await _module.InvokeVoidAsync("print");
    }
}
