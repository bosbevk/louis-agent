using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The trial's cost is at or under a limit (F18 step 4.2): the turns' costs from the stream, summed. A cost that isn't known is
/// Unknown, never a pass, and never a fail either: the grader can't tell.
/// </summary>
public sealed class CostUnderGrader(decimal limit, bool required = true) : IGrader
{
    public string Name => "cost-under";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown) return ValueTask.FromResult(unknown);
        decimal total = 0;
        foreach (var turn in trial.Transcript.Turns)
        {
            var cost = EngineEvents.CostOf(turn);
            if (cost is null) return ValueTask.FromResult(Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.CostUnknown)));
            total += cost.Value;
        }
        return ValueTask.FromResult(total <= limit
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.CostPass, new { Cost = total, Limit = limit }))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.CostOver, new { Cost = total, Limit = limit })));
    }
}
