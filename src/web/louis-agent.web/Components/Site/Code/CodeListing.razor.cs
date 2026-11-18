using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Site.Code;

/// <summary>
/// Code on a page (F164.1): a listing named for its file, or its language when it has none, the code shown as text so
/// a page that shows a program can never run it or break with it, with a button that copies it exactly as shown
/// (Louis: "this must be an actual code block you can copy paste"), and one that asks the site's agent about it
/// (F164.2).
/// </summary>
/// <remarks>
/// The copy goes through the listing's own module (ADR 0093), held and let go with the listing; its words, Copy and
/// Copied, are the site's own <see cref="ChatText"/>, so a site in another language says them in its own. The ask
/// goes through the door, as every Ask button's does (Observer), and is always drawn, since every site has an agent
/// (Louis: "All our sites will have agents").
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public partial class CodeListing : IAsyncDisposable
{
    /// <summary>The code, shown exactly as given.</summary>
    [Parameter, EditorRequired]
    public string Code { get; set; } = "";

    /// <summary>Its language, as a highlighter names it: <c>csharp</c>, <c>bash</c>, <c>json</c>.</summary>
    [Parameter, EditorRequired]
    public string Language { get; set; } = "";

    /// <summary>Its file's name, such as <c>Program.cs</c>, when it has one.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The site's words, the button's among them.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;

    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The listing's module, made on the first copy.</summary>
    private JsModule? _module;

    /// <summary>What the last press came to: none yet, copied, or refused by the browser.</summary>
    private bool? _copied;

    /// <summary>What the listing is named: its file, else its language.</summary>
    private string Label => Name ?? Language;

    /// <summary>The button's word: Copy, until a press says how it went.</summary>
    private string CopyWord => _copied switch
    {
        true => Text.Copied,
        false => Text.CopyFailed,
        null => Text.Copy
    };

    /// <summary>Copies the code, as it stands, and says how it went.</summary>
    private async Task CopyAsync()
    {
        _module ??= new JsModule(JS, FrameworkScripts.CodeListing);
        _copied = await _module.InvokeAsync<bool>("copy", Code);
    }

    /// <summary>
    /// Asks the site's agent to explain the code, named for its file, the code itself in the question, through the
    /// door, as an Ask button anywhere does (F164.2).
    /// </summary>
    private void Ask()
    {
        string question = string.Format(Text.ExplainCode, Label, Language, Code);
        Services.GetService<GuideDoor>()?.Ask(question);
    }

    /// <summary>Lets the module go with the listing.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }
}
