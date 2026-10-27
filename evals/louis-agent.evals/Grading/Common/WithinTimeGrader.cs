using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>Every turn of the trial was answered within a limit (F18 step 4.2), by the harness's clock.</summary>
public sealed class WithinTimeGrader(TimeSpan limit, bool required = true) : IGrader
{
    public string Name => "within-time";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);
        var slow = trial.Transcript.Turns.FirstOrDefault(turn => turn.Duration > limit);
        return ValueTask.FromResult(slow is null
            ? Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.WithinTimePass, new { Limit = limit.TotalSeconds }))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.SlowTurn,
                new { Turn = slow.Turn, Seconds = slow.Duration.TotalSeconds, Limit = limit.TotalSeconds })));
    }
}
