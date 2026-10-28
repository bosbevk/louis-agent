using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The trial's cost is at or under a limit in US dollars (F18 step 4.2): the turns' costs from the stream, summed. A turn that reported no
/// usage, a request with no price, or a price in another currency is Unknown, never a pass, and never a fail either: the grader can't tell.
/// </summary>
public sealed class CostUnderGrader(decimal limitUsd, bool required = true) : IGrader
{
    public string Name => "cost-under";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);
        decimal total = 0;
        foreach (var turn in trial.Transcript.Turns)
        {
            if (!EngineEvents.HasUsage(turn))
                return ValueTask.FromResult(Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.NoUsage, new { Turn = turn.Turn })));
            if (EngineEvents.CurrencyOf(turn) is { } currency &&
                !string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
                return ValueTask.FromResult(Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.OtherCurrency,
                        new { Turn = turn.Turn, Currency = currency })));
            var cost = EngineEvents.CostOf(turn);
            if (cost is null)
                return ValueTask.FromResult(Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.Unpriced, new { Turn = turn.Turn })));
            total += cost.Value;
        }

        return ValueTask.FromResult(total <= limitUsd
            ? Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.CostPass, new { Cost = total, Limit = limitUsd }))
            : Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.CostOver, new { Cost = total, Limit = limitUsd })));
    }
}
