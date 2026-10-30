using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The trial's cost is at or under a limit in US dollars: the turns' costs from the stream, summed. A turn that
/// reported no usage, a request with no price, or a price in another currency is Unknown, never a pass, and never a
/// fail either: the check can't tell.
/// </summary>
/// <remarks>
/// Its <see cref="Entry"/> builds it from a file's settings (Factory Method), nested here so the check and the way
/// it's named from a file sit in one place.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class CostUnderCheck(decimal limitUsd, bool required = true) : ICheck
{
    public string Name => "cost-under";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Adds up the turns' costs and compares the total with the limit, or says it can't tell.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);
        decimal total = 0;
        foreach (var turn in trial.Transcript.Turns)
        {
            if (!EngineEvents.HasUsage(turn))
                return ValueTask.FromResult(Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.NoUsage, new { turn.Turn })));
            if (EngineEvents.CurrencyOf(turn) is { } currency &&
                !string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
                return ValueTask.FromResult(Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.OtherCurrency,
                        new
                        {
                            turn.Turn,
                            Currency = currency
                        })));
            var cost = EngineEvents.CostOf(turn);
            if (cost is null)
                return ValueTask.FromResult(Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.Unpriced, new { turn.Turn })));
            total += cost.Value;
        }

        return ValueTask.FromResult(total <= limitUsd
            ? Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.CostPass, new
                {
                    Cost = total,
                    Limit = limitUsd
                }))
            : Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.CostOver, new
                {
                    Cost = total,
                    Limit = limitUsd
                })));
    }

    /// <summary>What a file can set: the limit in US dollars, and whether the check must pass.</summary>
    public sealed record Settings(decimal LimitUsd, bool Required = true);

    /// <summary>The check as a file names it, <c>cost-under</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("cost-under", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            new CostUnderCheck(settings.LimitUsd, settings.Required);
    }
}
