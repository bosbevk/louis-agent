using LouisAgent.Choices;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Evals;

/// <summary>
/// One eval's card on the evals' page: what it is, and its last run's number first. It shows only what the row
/// sends, so a card never claims more than the list knows.
/// </summary>
public partial class EvalCard
{
    /// <summary>The eval, with its last run in short.</summary>
    [Parameter, EditorRequired]
    public EvalRow Row { get; set; } = null!;

    /// <summary>"7 conversations".</summary>
    private string ConversationCount =>
        EvalsWords.Count(Row.Conversations, EvalsPart.Conversation, EvalsPart.Conversations);

    /// <summary>What a card says before the eval's first run.</summary>
    private static string NotRunYet => EvalsWords.Words[EvalsPart.NotRunYet];

    /// <summary>What a card says of a run that stopped before its end.</summary>
    private static string CouldNotFinish => EvalsWords.Words[EvalsPart.CouldNotFinish];

    /// <summary>Whether the last run stopped before its end.</summary>
    private bool Stopped => Row.LastRun is { Status: RunStatus.CouldNotFinish };

    /// <summary>When the last run started, in the browser's own time.</summary>
    private string When => Row.LastRun is { } run ? EvalsWords.FormatLocal(run.StartedAt) : "";

    /// <summary>Live or replay, by its wire name.</summary>
    private string Kind => Row.LastRun?.Kind.Wire() ?? "";

    /// <summary>"3 repeats".</summary>
    private string RepeatCount => EvalsWords.Count(Row.LastRun?.Repeats ?? 0, EvalsPart.Repeat, EvalsPart.Repeats);

    /// <summary>The last run's own page.</summary>
    private string Open => Row.LastRun is { } run ? EvalAddress.RunPageOf(Row.Name, run.Id) : "";
}
