using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The grades a grader returns (F18 step 4.4 review): the common graders and an identity's own grader build theirs with these, so a pass
/// scores 1, a fail and an unknown score 0, and an infrastructure failure is Unknown everywhere.
/// </summary>
public static class Verdict
{
    public static Grade Pass(string grader, int version, string reason) =>
        new(grader, version, 1, GradeOutcome.Pass, reason);

    public static Grade Fail(string grader, int version, string reason) =>
        new(grader, version, 0, GradeOutcome.Fail, reason);

    public static Grade Pending(string grader, int version, string reason) =>
        new(grader, version, 0, GradeOutcome.Pending, reason);

    public static Grade Unknown(string grader, int version, string reason) =>
        new(grader, version, 0, GradeOutcome.Unknown, reason);

    /// <summary>
    /// The Unknown grade of the first turn that failed for the infrastructure (a timeout, a dropped connection), or null when none did.
    /// The failure is reported apart: it is not the agent's, so no grader may call it a fail.
    /// </summary>
    public static Grade? InfrastructureFailure(string grader, int version, IEnumerable<TurnTranscript> turns)
    {
        var failed = turns.FirstOrDefault(turn => EngineEvents.InfrastructureFailure(turn) is not null);
        if (failed is null) return null;
        return Unknown(grader, version, MessageText.Format(CommonGradeMessage.InfrastructureFailure,
            new { Turn = failed.Turn, Failure = EngineEvents.InfrastructureFailure(failed) }));
    }
}
