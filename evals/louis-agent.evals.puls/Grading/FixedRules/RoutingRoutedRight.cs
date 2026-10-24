using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Puls.Routing;
using LouisAgent.Evals.Puls.Transcripts;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Grading.FixedRules;

/// <summary>The routing's route: each kind of question went where it should (F29). Required. A question is one turn.</summary>
public sealed class RoutingRoutedRight : IGrader
{
    public string Name => "routing-routed-right";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => true;

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        var turn = TurnView.From(trial.Transcript.Turns[0].Events);
        var scores = RoutingScorer.Score(trial.Scenario.Id, turn, RoutingScorer.Delegating(trial.Mode));
        return ValueTask.FromResult(scores.RoutedRight
            ? new Grade(Name, Version, 1, GradeOutcome.Pass, MessageText.Format(GradeMessage.RoutedWhereKindGoes))
            : new Grade(Name, Version, 0, GradeOutcome.Fail, RouteReason.Of(turn, scores.Role)));
    }
}
