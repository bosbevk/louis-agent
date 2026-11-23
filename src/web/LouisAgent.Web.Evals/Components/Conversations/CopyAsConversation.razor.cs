using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Evals.Components.Conversations;

/// <summary>
/// Copies a run's conversation as a block for its eval file, ready to paste and change: a real conversation becomes a
/// test case. The block is the API's, written by the eval file's own options, so its shape is said once.
/// </summary>
/// <remarks>
/// The copy reuses the framework's code listing module (<see cref="FrameworkScripts.CodeListing" />), held and let go
/// with the button (ADR 0093), rather than a script of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public partial class CopyAsConversation : IAsyncDisposable
{
    private JsModule? _module;
    private bool? _copied;
    private bool _copying;
    private string? _failed;

    /// <summary>The eval the run belongs to, whose file the block is for.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The run's id.</summary>
    [Parameter, EditorRequired]
    public string Run { get; set; } = "";

    /// <summary>The conversation's id.</summary>
    [Parameter, EditorRequired]
    public string Conversation { get; set; } = "";

    [Inject] private EvalsApi Api { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The button's word: Copy as conversation, then Copied once it was.</summary>
    private string Word => EvalsWords.Words[_copied == true ? EvalsPart.CopiedMark : EvalsPart.CopyAsConversation];

    /// <summary>What happened, after a press: copied for the file, refused by the browser, or the API's error.</summary>
    private string? Said => _copied switch
    {
        true => EvalsWords.Fill(EvalsPart.Copied, Eval),
        false => _failed ?? EvalsWords.Words[EvalsPart.CopyRefused],
        null => null
    };

    /// <summary>Reads the block and copies it.</summary>
    private async Task CopyAsync()
    {
        _copying = true;
        _failed = null;
        try
        {
            string block = await Api.FindAsFileAsync(Run, Conversation);
            _module ??= new JsModule(JS, FrameworkScripts.CodeListing);
            _copied = await _module.InvokeAsync<bool>("copy", block);
        }
        catch (AgentApiException failed)
        {
            _failed = failed.Message;
            _copied = false;
        }

        _copying = false;
    }

    /// <summary>The module goes with the button.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }
}
