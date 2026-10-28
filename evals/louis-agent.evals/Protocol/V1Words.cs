using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The words version 1 of the protocol uses for the contract's kinds, so the v1 writer and the runner's listing say
/// exactly what they said before the harness took the contract's kinds. The API reads these until step 7c.3, and
/// this class goes with it.
/// </summary>
/// <remarks>
/// One switch table per kind, read like a table: version 1 spelt a grader's kind and an outcome as the member's name
/// (<c>FixedRule</c>, <c>Pass</c>) and a two-word failure or split with an underscore (<c>api_error</c>,
/// <c>held_out</c>), where the contract writes lower-case kebab-case. Keeping v1's words in one place lets every
/// other line of the harness use the contract's kinds now.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class V1Words
{
    /// <summary>The old spellings of a turn's failure, so a v1 line still reads.</summary>
    public static readonly IReadOnlyDictionary<string, TurnFailure> FailureAliases =
        new Dictionary<string, TurnFailure>
        {
            ["api_error"] = TurnFailure.ApiError,
            ["replay_miss"] = TurnFailure.ReplayMiss,
        };

    /// <summary>A run's split as version 1 writes it: <c>held_out</c> with its underscore.</summary>
    public static string Split(RunSplit split) => split switch
    {
        RunSplit.HeldOut => "held_out",
        _ => split.Wire(),
    };

    /// <summary>A model's level as version 1 writes it: the member's name.</summary>
    public static string Level(ModelLevel level) => level switch
    {
        ModelLevel.Agent => "Agent",
        ModelLevel.Specialist => "Specialist",
        ModelLevel.Minion => "Minion",
        ModelLevel.Grader => "Grader",
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };

    /// <summary>A grader's kind as version 1 writes it: the member's name.</summary>
    public static string Kind(GraderKind kind) => kind switch
    {
        GraderKind.FixedRule => "FixedRule",
        GraderKind.Model => "Model",
        GraderKind.Human => "Human",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>A grade's outcome as version 1 writes it: the member's name.</summary>
    public static string Outcome(GradeOutcome outcome) => outcome switch
    {
        GradeOutcome.Pass => "Pass",
        GradeOutcome.Fail => "Fail",
        GradeOutcome.Unknown => "Unknown",
        GradeOutcome.Pending => "Pending",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };

    /// <summary>A conversation's outcome as version 1 writes it: the member's name.</summary>
    public static string Trial(TrialOutcome outcome) => outcome switch
    {
        TrialOutcome.Pass => "Pass",
        TrialOutcome.Fail => "Fail",
        TrialOutcome.Unknown => "Unknown",
        TrialOutcome.Infrastructure => "Infrastructure",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };

    /// <summary>A turn's failure as version 1 writes it: two words with an underscore.</summary>
    public static string Failure(TurnFailure failure) => failure switch
    {
        TurnFailure.ApiError => "api_error",
        TurnFailure.ReplayMiss => "replay_miss",
        _ => failure.Wire(),
    };
}
