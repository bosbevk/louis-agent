using System.Text.Json;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Grades.Reading;

namespace LouisAgent.Puls.Tests.Admin.Grades.Reading;

/// <summary>
/// The run's eval and graders as the portal reads them from the admin API (F18 step 4.5c): in order, with the eval's
/// version and commit, and nothing for a run from before step 2a. A conversation's grades are read from its typed row
/// (<see cref="ConversationGrades"/>), so they have their own tests.
/// </summary>
public class GradesReadersTests
{
    /// <summary>Parses a bit of JSON into a value we can keep.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>The graders come in the order the run listed them, with the eval's name, version and commit.</summary>
    [Test]
    public void TheRunsGradersComeInOrder_WithItsEvalVersionAndCommit()
    {
        var run = RunGradersReader.Read(Json("""
                                             {"run":{"id":"run_1","eval":"stub","eval_version":3,"commit":"abc1234",
                                                     "graders":[{"name":"no-error","version":1,"kind":"FixedRule"},
                                                                {"name":"meal","version":2,"kind":"Model"}]}}
                                             """));

        Assert.That((run.Eval, run.Version, run.Commit), Is.EqualTo(("stub", 3, "abc1234")));
        Assert.That(run.Graders,
            Is.EqualTo(new[] { new GraderRow("no-error", 1, "FixedRule"), new GraderRow("meal", 2, "Model") }));
    }

    /// <summary>A human review's question rides on its grader (F18 step 6a.3); the other graders have none.</summary>
    [Test]
    public void AHumanReviewsQuestion_IsReadWithItsGrader_AndAnotherGradersHasNone()
    {
        var run = RunGradersReader.Read(Json("""
                                             {"run":{"id":"run_1","eval":"puls-cook","eval_version":9,"commit":"abc",
                                                     "graders":[{"name":"no-error","version":1,"kind":"FixedRule",
                                                                 "question":null},
                                                                {"name":"meal.human","version":1,"kind":"Human",
                                                                 "question":"Is this a meal?"}]}}
                                             """));

        Assert.That(run.Graders,
            Is.EqualTo(new[]
            {
                new GraderRow("no-error", 1, "FixedRule"), new GraderRow("meal.human", 1, "Human", "Is this a meal?")
            }));
    }

    /// <summary>A run from before graders were recorded has no eval, version, commit or graders.</summary>
    [Test]
    public void ARunFromBeforeStep2a_HasNullGraders()
    {
        var run = RunGradersReader.Read(
            Json("""{"run":{"id":"run_0","eval":null,"eval_version":null,"commit":null,"graders":null}}"""));

        Assert.That(run, Is.EqualTo(new RunGraders(null, null, null, null)));
    }

    /// <summary>A run whose reply leaves the fields out altogether, not only null, has no graders either.</summary>
    [Test]
    public void ARunThatSendsNoSuchFields_HasNullGraders()
    {
        var run = RunGradersReader.Read(Json("""{"run":{"id":"run_0"}}"""));

        Assert.That(run.Graders, Is.Null);
    }

    /// <summary>A grader whose row carries no version is unversioned, never version 0 (ADR 0007).</summary>
    [Test]
    public void AGraderWithNoVersion_ReadsNull_Never0()
    {
        var run = RunGradersReader.Read(Json("""{"run":{"graders":[{"name":"meal","kind":"Human"}]}}"""));

        Assert.That(run.Graders!.Single(), Is.EqualTo(new GraderRow("meal", null, "Human")));
    }
}
