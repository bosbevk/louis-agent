using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Puls.Routing;
using LouisAgent.Evals.Puls.Turns;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Grading.FixedRules;

/// <summary>The routing's route: each kind of question went where it should (F29). Required. A question is one turn.</summary>
public sealed class RoutingRoutedRight : IGrader
{
    public string Name => "routing-routed-right";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => true;
    public IReadOnlySet<string> ExpectedKeys => FrozenSet<string>.Empty;

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        // A turn that failed for the infrastructure is not the agent's: Unknown, never a fail (F18 step 5.3).
        if (Verdict.InfrastructureFailure(Name, Version, [trial.Transcript.Turns[0]]) is { } unknown) return ValueTask.FromResult(unknown);

        var turn = TurnView.From(trial.Transcript.Turns[0].Events);
        var scores = RoutingScorer.Score(trial.Scenario.Id, turn, RoutingScorer.Delegating(trial.Mode));
        return ValueTask.FromResult(scores.RoutedRight
            ? new Grade(Name, Version, 1, GradeOutcome.Pass, MessageText.Format(GradeMessage.RoutedWhereKindGoes))
            : new Grade(Name, Version, 0, GradeOutcome.Fail, RouteReason.Of(turn, scores.Role)));
    }
}
