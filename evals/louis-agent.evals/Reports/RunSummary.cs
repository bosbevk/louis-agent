using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Outcomes;

namespace LouisAgent.Evals.Reports;

/// <summary>
/// A mode's trials rolled up (F18 step 5c): the overall line, then one line per category its scenarios carry, sorted by name. Aggregate and slice (GOAL skills 12 and 17): one
/// overall number hides where an agent fails, and the same trials cut by language, shop or budget show it. Pure and static, like <see cref="PassRates"/>, whose formulas it
/// uses and never copies.
/// <para>
/// <b>The rules.</b> A mode is one configuration of the agent, so trials of two modes are never mixed: the caller passes one mode's. A trial in two categories counts in both and
/// once in the overall line; one with none counts only there (so does a scenario the dataset no longer has, which a re-score gives no categories). <c>N</c> is every trial of the
/// slice; an Unknown trial is not passed, and an infrastructure one is left out of the rates, the grader counts and the score means (what it scored is the network's, not the
/// agent's) but counted. The rates are over the counted trials; <b>below <see cref="MinimumForRates"/> counted trials a slice has no rates</b> and says so (<c>TooFew</c>), because a
/// share of three trials looks precise and is not.
/// </para>
/// </summary>
public static class RunSummary
{
    /// <summary>The fewest counted trials a slice needs for a rate. Five, because a share of fewer moves by 20 points or more on one trial, so it would be read as more than it is.</summary>
    public const int MinimumForRates = 5;

    public const string Overall = "overall";

    /// <summary>The mode's lines: the overall one first, then each category by name (ordinal). No trials, no lines.</summary>
    public static IReadOnlyList<SummaryLine> Of(string mode, IReadOnlyList<TrialResult> trials,
        IReadOnlyList<IGrader> graders)
    {
        if (trials.Count == 0) return [];

        var required = graders.Where(grader => grader.Required).ToList();
        var lines = new List<SummaryLine> { Slice(mode, Overall, trials, required) };
        foreach (string category in trials.SelectMany(trial => trial.Categories).Distinct()
                     .Order(StringComparer.Ordinal))
            lines.Add(Slice(mode, category, trials.Where(trial => trial.Categories.Contains(category)).ToList(),
                required));
        return lines;
    }

    private static SummaryLine Slice(string mode, string slice, IReadOnlyList<TrialResult> trials,
        IReadOnlyList<IGrader> required)
    {
        var counted = trials.Where(trial => trial.Outcome != TrialOutcome.Infrastructure).ToList();
        int Count(TrialOutcome outcome) => trials.Count(trial => trial.Outcome == outcome);
        int passed = Count(TrialOutcome.Pass);
        bool tooFew = counted.Count < MinimumForRates;
        var share = tooFew
            ? null
            : PassRates.ShareOf(trials.GroupBy(trial => trial.Scenario)
                .Select(scenario => PassRates.Over([.. scenario.Select(trial => trial.Outcome)])));
        var graders = required.Select(grader => GraderCountOf(grader, counted)).ToList();
        var kinds = graders.GroupBy(count => count.Kind).OrderBy(group => group.Key)
            .Select(group => new KindCount(group.Key, group.Sum(c => c.Passed), group.Sum(c => c.Failed),
                group.Sum(c => c.Unknown))).ToList();

        return new SummaryLine(mode, slice, trials.Count, passed, Count(TrialOutcome.Fail), Count(TrialOutcome.Unknown),
            Count(TrialOutcome.Infrastructure),
            [.. trials.Select(trial => trial.Scenario).Distinct().Order(StringComparer.Ordinal)], tooFew,
            tooFew ? null : (double)passed / counted.Count, share?.PassAtK, share?.PassHatK, graders, kinds,
            ScoreMeans.Of(counted.Select(trial => trial.Scores)));
    }

    /// <summary>One grader's counts over the counted trials; a trial that has no grade from it counts as Unknown for it.</summary>
    private static GraderCount GraderCountOf(IGrader grader, IReadOnlyList<TrialResult> counted)
    {
        var outcomes = counted
            .Select(trial => trial.Grades.FirstOrDefault(grade => grade.Grader == grader.Name)?.Outcome).ToList();
        return new GraderCount(grader.Name, grader.Kind, outcomes.Count(o => o == GradeOutcome.Pass),
            outcomes.Count(o => o == GradeOutcome.Fail),
            outcomes.Count(o => o is null or GradeOutcome.Unknown));
    }
}
