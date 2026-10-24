using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Cook;
using LouisAgent.Evals.Puls.Transcripts;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Grading.FixedRules;

/// <summary>The cook's route: a meal went to the cook, and the cook's mode is the one that had it (F31). Required: a trial passes only with it.</summary>
public sealed class CookRoutedRight : IGrader
{
    public string Name => "cook-routed-right";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => true;

    /// <summary>The cook's mode is the one with the cook: only "puls-cook" runs the cook, the rest answer themselves.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        var turns = trial.Transcript.Turns.Select(t => TurnView.From(t.Events)).ToList();
        var scores = CookScorer.Score(trial.Scenario.Id, turns, cookOn: trial.Mode.Name == "puls-cook");
        return ValueTask.FromResult(scores.RoutedRight
            ? new Grade(Name, Version, 1, GradeOutcome.Pass, "routed to the cook")
            : new Grade(Name, Version, 0, GradeOutcome.Fail, $"handled as {scores.Role ?? "by Puls itself"}"));
    }
}
