using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Grades;

namespace LouisAgent.Puls.Tests.Admin.Grades;

/// <summary>
/// The run's eval and graders as the portal shows them, read from the contract's run row (F18 step 4.5c): in order,
/// with the eval's version and commit, and nothing for a run from before graders were recorded.
/// </summary>
public class RunGradersTests
{
    /// <summary>A finished run's row with the eval fields given, everything else plain.</summary>
    private static StoredRunRow Run(string? eval, int? version, string? commit, IReadOnlyList<RunGraderRow>? graders) =>
        new("run_1", "check-x", 1, "done", 0, DateTimeOffset.UnixEpoch, null, null, null, null, eval, version, commit,
            null, graders);

    /// <summary>The graders come in the order the run listed them, with the eval's name, version and commit.</summary>
    [Test]
    public void TheRunsGradersComeInOrder_WithItsEvalVersionAndCommit()
    {
        var run = Run("stub", 3, "abc1234",
            [new RunGraderRow("no-error", 1, "fixed-rule", null), new RunGraderRow("meal", 2, "model", null)]);

        var graders = RunGraders.Of(run);

        Assert.That((graders.Eval, graders.Version, graders.Commit), Is.EqualTo(("stub", 3, "abc1234")));
        Assert.That(graders.Graders,
            Is.EqualTo(new[] { new GraderRow("no-error", 1, "fixed-rule"), new GraderRow("meal", 2, "model") }));
    }

    /// <summary>A human review's question rides on its grader (F18 step 6a.3); the other graders have none.</summary>
    [Test]
    public void AHumanReviewsQuestion_IsReadWithItsGrader_AndAnotherGradersHasNone()
    {
        var run = Run("an-eval", 9, "abc",
        [
            new RunGraderRow("no-error", 1, "fixed-rule", null),
            new RunGraderRow("meal.human", 1, "human", "Is this a meal?"),
        ]);

        Assert.That(RunGraders.Of(run).Graders,
            Is.EqualTo(new[]
            {
                new GraderRow("no-error", 1, "fixed-rule"),
                new GraderRow("meal.human", 1, "human", "Is this a meal?")
            }));
    }

    /// <summary>A run from before graders were recorded has no eval, version, commit or graders.</summary>
    [Test]
    public void ARunFromBeforeStep2a_HasNullGraders() =>
        Assert.That(RunGraders.Of(Run(null, null, null, null)), Is.EqualTo(new RunGraders(null, null, null, null)));
}
