using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading;

/// <summary>No turn of the conversation failed: an errored turn is never right (F31). Required.</summary>
public sealed class CookNoError : ICheck
{
    public string Name => "cook-no-error";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => true;
    public IReadOnlySet<string> ExpectedKeys => FrozenSet<string>.Empty;

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        // A turn that failed for the infrastructure is not the agent's: Unknown, never a fail (F18 step 5.3).
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);

        int errors = trial.Transcript.Turns.Count(EngineEvents.StreamedError);
        return ValueTask.FromResult(errors == 0
            ? new Grade(Name, Version, 1, GradeOutcome.Pass, MessageText.Format(GradeMessage.NoTurnFailed))
            : new Grade(Name, Version, 0, GradeOutcome.Fail,
                MessageText.Format(GradeMessage.TurnsFailed, new { Count = errors })));
    }
}
