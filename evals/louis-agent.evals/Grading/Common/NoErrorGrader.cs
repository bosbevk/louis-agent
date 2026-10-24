using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>No turn of the trial raised an error (F18 step 4.2): the first thing every eval checks.</summary>
public sealed class NoErrorGrader(bool required = true) : IGrader
{
    public string Name => "no-error";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        var errored = trial.Transcript.Turns.FirstOrDefault(EngineEvents.RaisedError);
        return ValueTask.FromResult(errored is null
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.NoErrorPass))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.ErrorInTurn, new { Turn = errored.Turn })));
    }
}
