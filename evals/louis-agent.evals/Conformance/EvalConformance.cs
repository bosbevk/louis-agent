using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Conformance;

/// <summary>
/// The conformance check every eval must pass (F18 step 4.2b): its shape (a name safe as an MCP tool name, a version of at least 1,
/// a required fixed rule, every expected key read by a grader), every grade (a reason, a score from 0 to 1, an outcome that agrees
/// with its score), and each scenario's reference transcripts (the reference passes the required graders, the known-bad one fails
/// them). Returns the failures, each as a sentence a person reads.
/// </summary>
public static class EvalConformance
{
    /// <summary>An MCP tool name: letters, digits, '-' and '_', at most 64.</summary>
    private static readonly Regex McpName = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.CultureInvariant);

    public static async ValueTask<IReadOnlyList<string>> CheckAsync(IEval eval, IReadOnlyList<ConformanceCase> cases,
        CancellationToken cancellationToken)
    {
        var failures = new List<string>();
        CheckShape(eval, failures);
        var required = eval.Graders.Where(grader => grader.Required).ToList();

        foreach (EvalScenario scenario in eval.Scenarios)
        {
            var conformanceCase = cases.FirstOrDefault(c => c.Scenario == scenario.Id);
            if (conformanceCase is null)
            {
                failures.Add(MessageText.Format(ConformanceMessage.NoCase, new { Scenario = scenario.Id }));
                continue;
            }

            var reference = await GradeAllAsync(eval, Trial(scenario, conformanceCase.Mode, conformanceCase.Reference), failures, cancellationToken);
            foreach (IGrader grader in required)
            {
                // A grader that threw is reported as a throw; it has no grade to fail the reference with.
                var grade = reference.FirstOrDefault(g => g.Grader == grader.Name);
                if (grade is { Outcome: not GradeOutcome.Pass })
                    failures.Add(MessageText.Format(ConformanceMessage.ReferenceFails,
                        new { Scenario = scenario.Id, Grader = grader.Name, Reason = grade.Reason }));
            }

            var knownBad = await GradeAllAsync(eval, Trial(scenario, conformanceCase.Mode, conformanceCase.KnownBad), failures, cancellationToken);
            var requiredBad = knownBad.Where(g => required.Any(r => r.Name == g.Grader)).ToList();
            if (required.Count > 0 && requiredBad.Count == required.Count && requiredBad.All(g => g.Outcome == GradeOutcome.Pass))
                failures.Add(MessageText.Format(ConformanceMessage.BadPasses, new { Scenario = scenario.Id }));
        }
        return failures;
    }

    private static Trial Trial(EvalScenario scenario, EvalMode mode, Transcript transcript) =>
        new("conformance", mode, 1, scenario, transcript);

    private static void CheckShape(IEval eval, List<string> failures)
    {
        if (!McpName.IsMatch(eval.Name)) failures.Add(MessageText.Format(ConformanceMessage.NameNotSafe, new { Name = eval.Name }));
        if (eval.Version < 1) failures.Add(MessageText.Format(ConformanceMessage.VersionBelowOne, new { Name = eval.Name, Version = eval.Version }));
        if (!eval.Graders.Any(grader => grader.Required && grader.Kind == GraderKind.FixedRule))
            failures.Add(MessageText.Format(ConformanceMessage.NoRequiredFixedRule, new { Name = eval.Name }));

        var keys = eval.Graders.SelectMany(grader => grader.ExpectedKeys).ToHashSet();
        foreach (EvalScenario scenario in eval.Scenarios)
        {
            if (scenario.Expected.ValueKind != JsonValueKind.Object) continue;
            foreach (JsonProperty criterion in scenario.Expected.EnumerateObject())
                if (!keys.Contains(criterion.Name))
                    failures.Add(MessageText.Format(ConformanceMessage.UnreadKey, new { Scenario = scenario.Id, Key = criterion.Name }));
        }
    }

    /// <summary>Every grader's grade on the trial, each checked; a grader that throws is reported and has no grade.</summary>
    private static async Task<List<Grade>> GradeAllAsync(IEval eval, Trial trial, List<string> failures, CancellationToken cancellationToken)
    {
        var grades = new List<Grade>();
        foreach (IGrader grader in eval.Graders)
        {
            Grade grade;
            try
            {
                grade = await grader.GradeAsync(trial, cancellationToken);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                failures.Add(MessageText.Format(ConformanceMessage.GraderThrew, new { Grader = grader.Name, Reason = error.Message }));
                continue;
            }
            CheckGrade(grade, failures);
            grades.Add(grade);
        }
        return grades;
    }

    private static void CheckGrade(Grade grade, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(grade.Reason)) failures.Add(MessageText.Format(ConformanceMessage.NoReason, new { Grader = grade.Grader }));
        if (grade.Score < 0 || grade.Score > 1)
            failures.Add(MessageText.Format(ConformanceMessage.ScoreOutOfRange, new { Grader = grade.Grader, Score = grade.Score }));
        bool agrees = grade.Outcome == GradeOutcome.Pass ? grade.Score == 1 : grade.Score == 0;
        if (!agrees)
            failures.Add(MessageText.Format(ConformanceMessage.OutcomeScore, new { Grader = grade.Grader, Outcome = grade.Outcome, Score = grade.Score }));
    }
}
