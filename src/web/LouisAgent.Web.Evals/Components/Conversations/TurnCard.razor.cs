using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Conversations;

/// <summary>
/// One turn of a repeat, the way a person reads a transcript: the ask, the answer, and its marks, with the tool
/// calls folded away until they are wanted.
/// </summary>
public partial class TurnCard
{
    /// <summary>The turn's number in its repeat, from 1.</summary>
    [Parameter, EditorRequired]
    public int Number { get; set; }

    /// <summary>The turn.</summary>
    [Parameter, EditorRequired]
    public TurnRun Turn { get; set; } = null!;

    /// <summary>"2 tool calls · ReadDoc, ShowPage": how many, and the tools by name, each once.</summary>
    private string CallsSummary =>
        $"{EvalsWords.Count(Turn.Transcript.Calls.Count, EvalsPart.ToolCall, EvalsPart.ToolCalls)} · " +
        string.Join(", ", Turn.Transcript.Calls.Select(call => call.Tool).Distinct());
}
