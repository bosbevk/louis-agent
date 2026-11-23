using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Components.Marks;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using LouisAgent.Web.Usage;
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

    /// <summary>The run the turn is in, its id.</summary>
    [Parameter, EditorRequired]
    public string Run { get; set; } = "";

    /// <summary>The conversation the turn is in, its id.</summary>
    [Parameter, EditorRequired]
    public string Conversation { get; set; } = "";

    /// <summary>The repeat the turn is in, its number.</summary>
    [Parameter, EditorRequired]
    public int Repeat { get; set; }

    /// <summary>Hands up a flip of one of its criteria (F126.4).</summary>
    [Parameter]
    public EventCallback<FlipAsk> OnFlip { get; set; }

    /// <summary>Hands up an undo of one of its criteria's flip.</summary>
    [Parameter]
    public EventCallback<MarkPlace> OnUndo { get; set; }

    /// <summary>Where its criterion, counted from 1, is in the run.</summary>
    private MarkPlace PlaceOf(int criterion) => new(Run, Conversation, Repeat, Number, criterion);

    private static string AgentCostName => EvalsWords.Words[EvalsPart.AgentCostName];

    private static string JudgeCostName => EvalsWords.Words[EvalsPart.JudgeCostName];

    /// <summary>What the turn cost the agent, or unknown, never 0.</summary>
    private string AgentCost => MoneyOf(Turn.Cost);

    /// <summary>What judging the turn's criteria cost, or unknown, never 0 (F126.3.4).</summary>
    private string JudgeCost => MoneyOf(Turn.JudgeCost);

    private string MoneyOf(decimal? cost) =>
        cost is { } amount ? UsageFormat.Money(amount, Turn.Currency) : EvalsWords.Words[EvalsPart.Unknown];

    /// <summary>"2 tool calls · ReadDoc, ShowPage": how many, and the tools by name, each once.</summary>
    private string CallsSummary =>
        $"{EvalsWords.Count(Turn.Transcript.Calls.Count, EvalsPart.ToolCall, EvalsPart.ToolCalls)} · " +
        string.Join(", ", Turn.Transcript.Calls.Select(call => call.Tool).Distinct());
}
