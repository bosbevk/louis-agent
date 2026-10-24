using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Cook;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Grading.FixedRules;

/// <summary>The cook's route: a meal went to the cook, and the cook's mode is the one that had it (F31). Required: a trial passes only with it.</summary>
public sealed class CookRoutedRight : IGrader
{
    public string Name => "cook-routed-right";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => true;

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        var scores = CookScorer.ScoreTrial(trial);
        return ValueTask.FromResult(scores.RoutedRight
            ? new Grade(Name, Version, 1, GradeOutcome.Pass, "routed to the cook")
            : new Grade(Name, Version, 0, GradeOutcome.Fail, (scores.Role is null ? "answered by Puls itself" : $"handled by {scores.Role}")));
    }
}
