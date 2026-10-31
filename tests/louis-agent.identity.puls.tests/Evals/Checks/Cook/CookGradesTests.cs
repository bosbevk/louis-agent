using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook;

/// <summary>
/// Every grade the cook's checks give on every one of its reference and known-bad transcripts, pinned in a golden
/// file: the transcript, the check, its outcome and its score, never its reason. When a check is swapped for another
/// that should grade the same (F49.2b step 6: Puls's checks for the general ones), this is the proof that it does.
/// </summary>
public class CookGradesTests
{
    /// <summary>The golden file, beside this test in the repository.</summary>
    private static readonly string GoldenFile = Path.Combine(TestPaths.RepoRoot, "tests",
        "louis-agent.identity.puls.tests", "Evals", "Checks", "Cook", "CookGrades.txt");

    /// <summary>
    /// The grades match the golden file. A deliberate change is rewritten with <c>LOUIS_UPDATE_GOLDEN=1</c>, and its
    /// diff reviewed before it is committed.
    /// </summary>
    [Test]
    public async Task EveryGrade_OnEveryTranscript_MatchesTheGoldenFile()
    {
        var actual = await SnapshotAsync();
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            await File.WriteAllTextAsync(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        Assert.That(actual, Is.EqualTo((await File.ReadAllTextAsync(GoldenFile)).ReplaceLineEndings("\n")),
            "A cook grade changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    /// <summary>
    /// One line per grade, transcript by transcript in the order the cases load, each check in the eval's order; a
    /// person's review that doesn't apply to the trial is left out, as a run leaves it out.
    /// </summary>
    private static async Task<string> SnapshotAsync()
    {
        var eval = CookFile.Eval;
        var lines = new List<string>();
        foreach (var conformanceCase in ConformanceCases.Load(eval.Transcripts, eval.Scenarios))
        {
            var scenario = eval.Scenarios.Single(s => s.Id == conformanceCase.Scenario);
            List<(string Label, Transcript Transcript)> transcripts =
            [
                ("reference", conformanceCase.Reference),
                .. conformanceCase.KnownBad.Select(bad => ($"bad.{bad.Label}", bad.Transcript))
            ];
            foreach (var (label, transcript) in transcripts)
            {
                var trial = new Trial("grades", conformanceCase.Mode, 1, scenario, transcript);
                foreach (var check in eval.Checks)
                {
                    if (check is HumanReviewCheck review && !review.AppliesTo(trial)) continue;
                    var grade = await check.GradeAsync(trial, CancellationToken.None);
                    lines.Add(string.Join(" ", $"{scenario.Id}.{label}", grade.Check, grade.Outcome.Wire(),
                        grade.Score.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        return string.Join("\n", lines) + "\n";
    }
}
