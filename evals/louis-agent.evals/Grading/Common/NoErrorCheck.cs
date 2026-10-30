using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// No turn of the trial raised an error (F18 step 4.2): the agent's error in the stream, with its message in the reason. A specialist's
/// error is not the turn's, and a turn that failed for the infrastructure is Unknown, not a fail.
/// </summary>
public sealed class NoErrorCheck(bool required = true) : ICheck
{
    public string Name => "no-error";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        var errored = trial.Transcript.Turns.FirstOrDefault(EngineEvents.StreamedError);
        if (errored is not null)
        {
            string? message = EngineEvents.ErrorMessageOf(errored);
            string reason = message is null
                ? MessageText.Format(CommonGradeMessage.ErrorInTurn, new { Turn = errored.Turn })
                : MessageText.Format(CommonGradeMessage.ErrorInTurnWithMessage,
                    new { Turn = errored.Turn, Message = message });
            return ValueTask.FromResult(Verdict.Fail(Name, Version, reason));
        }

        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);
        return ValueTask.FromResult(Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.NoErrorPass)));
    }
}
