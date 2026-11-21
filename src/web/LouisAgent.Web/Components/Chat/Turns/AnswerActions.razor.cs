using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Chat.Turns;

/// <summary>Under a finished answer: copy, regenerate, good and bad, share, and its sources.</summary>
public partial class AnswerActions : IAsyncDisposable
{
    private JsModule? _shell;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The shell's module (CL31), shared with every part of the chat; the browser fetches it once.</summary>
    private JsModule Shell => _shell ??= new JsModule(JS, FrameworkScripts.Shell);

    [Inject] private ChatText Text { get; set; } = null!;

    [Parameter, EditorRequired] public Turn Turn { get; set; } = default!;
    [Parameter] public EventCallback<string> OnCopy { get; set; }
    [Parameter] public EventCallback<string> OnComingSoon { get; set; }

    /// <summary>Whether what only says it's coming shows too.</summary>
    [Parameter]
    public bool Unfinished { get; set; } = true;

    /// <summary>The answer's own words: its text after the last tool call or thought.</summary>
    private static string AnswerText(Turn turn) => SpecialistAnswer.Split(turn).Answer.Trim();

    /// <summary>Shows the work panel, where an answer's sources are.</summary>
    private Task ShowWork() => Shell.InvokeVoidAsync("showWork").AsTask();

    /// <summary>Lets the shell's module go, if it was ever loaded.</summary>
    public ValueTask DisposeAsync() => _shell?.DisposeAsync() ?? ValueTask.CompletedTask;
}
