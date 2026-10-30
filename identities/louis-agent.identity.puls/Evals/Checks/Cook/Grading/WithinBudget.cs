using System.Collections.Frozen;
using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;
using LouisAgent.Identities.Puls.Evals.Checks.Messages;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Turns;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;

/// <summary>
/// The first turn's basket costs no more than the scenario's <c>budget</c> in rand. The total is read from the answer
/// by <see cref="Prices.Total"/>, from the line that says it or the sum of the priced rows, and the reason says which.
/// An answer with no amount it can read is Unknown, never a guess. Required where a scenario sets a budget; a scenario
/// that sets none passes.
/// </summary>
/// <remarks>
/// Its <see cref="Entry"/> lets an eval file name it, <c>within-budget</c> (Factory Method), as the general checks'
/// entries do; it takes no settings.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class WithinBudget : ICheck
{
    public string Name => "within-budget";
    public int Version => 2;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => true;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "budget" }.ToFrozenSet();

    /// <summary>Compares the first turn's total with the scenario's budget.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown with no turn, a broken one or no amount to read; a pass with no budget set; then within or over.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
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
            Source = MessageText.Format(total.Source switch
            {
                TotalSource.TotalLine => GradeMessage.SourceTotalLine,
                TotalSource.ListTotalRow => GradeMessage.SourceListTotalRow,
                _ => GradeMessage.SourceRowSum,
            }),
        };
        return total.Amount <= budget
            ? Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.BudgetWithin, values))
            : Verdict.Fail(Name, Version, MessageText.Format(GradeMessage.BudgetOver, values));
    }

    /// <summary>What a file can set: nothing yet; the budget is the scenario's.</summary>
    public sealed record Settings;

    /// <summary>The check as a file names it, <c>within-budget</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("within-budget", CheckKind.FixedRule)
    {
        /// <summary>Builds the check.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => new WithinBudget();
    }
}
