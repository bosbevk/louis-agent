using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Reports;

namespace LouisAgent.Evals.Running;

/// <summary>
/// What one mode's trials came to so far (F18 step 5a): each scenario's outcomes over the repeats run, the trials left out for the
/// infrastructure, and the trials run a second time. Its rates are <see cref="PassRates"/>'s, over the repeats so far, so every
/// <c>mode_done</c> line is true for the trials before it and the last one is the mode's.
/// </summary>
public sealed class ModeTally
{
    private readonly Dictionary<string, List<TrialOutcome>> _outcomes = [];

    /// <summary>The trials run a second time after a transport failure.</summary>
    public int Retried { get; private set; }

    /// <summary>The trials left out of the rates: a failure of the infrastructure, not the agent's.</summary>
    public int Infrastructure => _outcomes.Values.Sum(outcomes => outcomes.Count(outcome => outcome == TrialOutcome.Infrastructure));

    /// <summary>The mode's pass@k and pass^k over the trials so far; null when none counts.</summary>
    public PassShare Share => PassRates.ShareOf(_outcomes.Values.Select(outcomes => PassRates.Over(outcomes)));

    /// <summary>Every graded trial of the mode, in order: what <see cref="RunSummary"/> rolls up (F18 step 5c).</summary>
    public IReadOnlyList<TrialResult> Results => _results;

    private readonly List<TrialResult> _results = [];

    /// <summary>A graded trial: its outcome counts as in 5a, and the trial is kept for the roll-up.</summary>
    public void Add(TrialResult result)
    {
        Add(result.Scenario, result.Outcome);
        _results.Add(result);
    }

    public void Add(string scenario, TrialOutcome outcome)
    {
        if (!_outcomes.TryGetValue(scenario, out var outcomes)) _outcomes[scenario] = outcomes = [];
        outcomes.Add(outcome);
    }

    public void CountRetry() => Retried++;

    /// <summary>A re-score keeps the count the stored run recorded (F18 step 5b): it ran no trial, so it retried none.</summary>
    public void CarryRetried(int retried) => Retried = retried;
}
