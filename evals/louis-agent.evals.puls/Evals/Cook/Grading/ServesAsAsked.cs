using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading;

/// <summary>
/// Each turn's answer names the number of people the scenario asks for (F18 step 5.4: <c>serves</c>, a number for the first turn or one per
/// turn). Not required: its job until step 6's model grader checks that the quantities scale is to catch the answer that forgot the number. A turn
/// with nothing to read (it failed, or answered nothing) leaves it Unknown, never a fail.
/// </summary>
public sealed class ServesAsAsked : IGrader
{
    public string Name => "serves-as-asked";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => false;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "serves" }.ToFrozenSet();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;

        var asked = CookExpectations.ServesOf(trial.Scenario);
        if (asked.Count == 0) return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ServesNotAsked));

        for (int i = 0; i < asked.Count; i++)
        {
            int number = i + 1;
            var view = i < turns.Count ? TurnView.From(turns[i]) : null;
            if (view is null || view.Error is not null || view.Answer.Length == 0)
                return Verdict.Unknown(Name, Version,
                    MessageText.Format(GradeMessage.ServesNothingToRead, new { Turn = number }));

            var found = Servings.Of(view.Answer);
            if (found.Contains(asked[i])) continue;
            return Verdict.Fail(Name, Version, found.Count == 0
                ? MessageText.Format(GradeMessage.ServesForgotten, new { Turn = number, Asked = asked[i] })
                : MessageText.Format(GradeMessage.ServesWrong,
                    new { Turn = number, Found = string.Join(", ", found), Asked = asked[i] }));
        }

        return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ServesRight));
    }
}
