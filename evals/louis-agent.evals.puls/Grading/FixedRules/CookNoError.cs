using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Cook;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Grading.FixedRules;

/// <summary>No turn of the conversation failed: an errored turn is never right (F31). Required.</summary>
public sealed class CookNoError : IGrader
{
    public string Name => "cook-no-error";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => true;

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        int errors = CookScorer.ScoreTrial(trial).Errors;
        return ValueTask.FromResult(errors == 0
            ? new Grade(Name, Version, 1, GradeOutcome.Pass, "no turn failed")
            : new Grade(Name, Version, 0, GradeOutcome.Fail, $"{errors} turn(s) failed"));
    }
}
