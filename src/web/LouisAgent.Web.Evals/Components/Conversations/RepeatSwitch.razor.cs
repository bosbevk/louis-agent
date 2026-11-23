using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Evals.Client;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Conversations;

/// <summary>Picks which of a conversation's repeats is on show, by linking each one's own address.</summary>
public partial class RepeatSwitch
{
    /// <summary>The eval the run belongs to.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The run's id.</summary>
    [Parameter, EditorRequired]
    public string Run { get; set; } = "";

    /// <summary>The conversation whose repeats it switches between.</summary>
    [Parameter, EditorRequired]
    public ConversationRun Conversation { get; set; } = null!;

    /// <summary>The number of the repeat on show.</summary>
    [Parameter, EditorRequired]
    public int Open { get; set; }

    private bool IsOpen(RepeatRun repeat) => repeat.Number == Open;

    private string PageOf(RepeatRun repeat) =>
        EvalAddress.ConversationPageOf(Eval, Run, Conversation.Id, repeat.Number);
}
