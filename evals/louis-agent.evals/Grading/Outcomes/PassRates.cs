using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Outcomes;

/// <summary>
/// A trial's outcome from its grades (F18 step 4.5a), and pass@k and pass^k over a scenario's trials (step 5a). Pure and static: it holds
/// the one copy of the formula, so the runner writes the rates and the API and the portal only read them (a second copy would drift).
/// <para>
/// <b>The words.</b> A <i>trial</i> is one run of one scenario; with <c>--repeat k</c> each scenario of a mode has k of them.
/// <b>pass@k</b>: the scenario passed at least once in its k trials ("can the agent do this?"). <b>pass^k</b>: it passed every time
/// ("can a customer rely on it?"; a sold agent needs this one, GOAL skill 12). A mode's rates are the share of its scenarios that
/// pass that way. An Unknown trial never counts as a pass. An <i>infrastructure</i> trial (a timeout, a dropped connection, an API
/// error, a replay miss) is neither a pass nor a fail: it is left out of k and reported apart, so a flaky network never counts as the
/// agent failing. A scenario with no countable trial is no part of the share, and a mode with none has no rates: null, never 0 (ADR 0007).
/// With one trial pass@1 and pass^1 are the same number.
/// </para>
/// <para>
/// This is the "any of k" form, exact when every trial is counted. The estimator of the HumanEval paper (n samples, pick k) is for
/// n greater than k, which we don't run.
/// </para>
/// </summary>
public static class PassRates
{
    /// <summary>
    /// A trial's outcome: Infrastructure when a turn failed for the infrastructure, whatever its grades say (a failed turn's grades are
    /// Unknown at best); otherwise <see cref="Of(IReadOnlyList{Grade}, IReadOnlyList{ICheck})"/>.
    /// </summary>
    public static TrialOutcome Of(Trial trial, IReadOnlyList<Grade> grades, IReadOnlyList<ICheck> graders) =>
        trial.Transcript.Turns.Any(turn => EngineEvents.InfrastructureFailure(turn) is not null)
            ? TrialOutcome.Infrastructure
            : Of(grades, graders);

    /// <summary>
    /// A trial's outcome from its grades: only the required graders count. Fail when any required grader failed, even beside an
    /// Unknown; otherwise Unknown when any required grader is Unknown or has no grade; otherwise Pass. With no required grader it is
    /// Unknown, so no trial passes without a check (the catalogue refuses such an eval anyway).
    /// </summary>
    public static TrialOutcome Of(IReadOnlyList<Grade> grades, IReadOnlyList<ICheck> graders)
    {
        var required = graders.Where(grader => grader.Required).ToList();
        if (required.Count == 0) return TrialOutcome.Unknown;

        var outcomes = required.Select(grader => grades.FirstOrDefault(grade => grade.Check == grader.Name)?.Outcome)
            .ToList();
        if (outcomes.Any(outcome => outcome == GradeOutcome.Fail)) return TrialOutcome.Fail;
        if (outcomes.Any(outcome => outcome is null or GradeOutcome.Unknown or GradeOutcome.Pending))
            return TrialOutcome.Unknown;
        return TrialOutcome.Pass;
    }

    /// <summary>One scenario's rates over its trials' outcomes: 1 or 0 each (it passed that way or it didn't), or null when no trial counts.</summary>
    public static PassRate Over(IReadOnlyList<TrialOutcome> outcomes)
    {
        var counted = outcomes.Where(outcome => outcome != TrialOutcome.Infrastructure).ToList();
        if (counted.Count == 0) return new PassRate(null, null, 0);
        return new PassRate(counted.Any(outcome => outcome == TrialOutcome.Pass) ? 1 : 0,
            counted.All(outcome => outcome == TrialOutcome.Pass) ? 1 : 0, counted.Count);
    }

    /// <summary>A mode's rates: the share of its scenarios that passed at least once, and the share that passed every time; a scenario with no countable trial is left out.</summary>
    public static PassShare ShareOf(IEnumerable<PassRate> scenarios)
    {
        var counted = scenarios.Where(rate => rate.PassAtK is not null && rate.PassHatK is not null).ToList();
        return counted.Count == 0
            ? new PassShare(null, null)
            : new PassShare(counted.Average(rate => rate.PassAtK!.Value),
                counted.Average(rate => rate.PassHatK!.Value));
    }
}
