using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Cook;
using LouisAgent.Evals.Puls.Messages;
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
    public IReadOnlySet<string> ExpectedKeys => FrozenSet<string>.Empty;

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        var scores = CookScorer.ScoreTrial(trial);
        return ValueTask.FromResult(scores.RoutedRight
            ? new Grade(Name, Version, 1, GradeOutcome.Pass, MessageText.Format(GradeMessage.RoutedToCook))
            : new Grade(Name, Version, 0, GradeOutcome.Fail, RouteReason.Of(TurnView.From(trial.Transcript.Turns[0].Events), scores.Role)));
    }
}
