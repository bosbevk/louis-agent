using System.Collections.Frozen;
using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Puls.Expectations;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Puls.Scoring;
using LouisAgent.Evals.Puls.Turns;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Grading.FixedRules;

/// <summary>
/// The first turn's basket costs no more than the scenario's <c>budget</c> in rand (F18 step 5.4). The total is read from the answer by
/// <see cref="Prices.Total"/>, from the line that says it or the sum of the priced rows, and the reason says which. An answer with no amount it can
/// read is Unknown, never a guess. Required where a scenario sets a budget; a scenario that sets none passes.
/// </summary>
public sealed class WithinBudget : IGrader
{
    public string Name => "within-budget";
    public int Version => 1;
    public GraderKind Kind => GraderKind.FixedRule;
    public bool Required => true;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "budget" }.ToFrozenSet();

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    private Grade Grade(Trial trial)
    {
        var first = trial.Transcript.Turns.FirstOrDefault();
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(GradeMessage.BudgetNoAmount));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;

        if (CookExpectations.BudgetOf(trial.Scenario) is not { } budget)
            return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.BudgetNotSet));

        var view = TurnView.From(first);
        if (Prices.Total(view.Answer) is not { } total)
            return Verdict.Unknown(Name, Version, MessageText.Format(GradeMessage.BudgetNoAmount));

        var values = new
        {
            Total = total.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            Budget = budget.ToString("0.##", CultureInfo.InvariantCulture),
            Source = MessageText.Format(total.Source == TotalSource.TotalLine ? GradeMessage.SourceTotalLine : GradeMessage.SourceRowSum),
        };
        return total.Amount <= budget
            ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.BudgetWithin, values))
            : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.BudgetOver, values));
    }
}
