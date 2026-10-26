using System.Text.Json;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Grades.Reading;

namespace LouisAgent.Puls.Tests.Admin.Grades.Reading;

// The readers of the admin API's standard records (F18 step 4.5c): the run's eval and graders in order, a conversation's grades and
// outcome, and null for a run or a conversation from before step 2a. Written first: both readers threw NotImplementedException on the
// red run; the no-score test (the 4.5c review) read 0 on its red run.
public class GradesReadersTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    [Test]
    public void TheRunsGradersComeInOrder_WithItsEvalVersionAndCommit()
    {
        var run = RunGradersReader.Read(Json("""
            {"run":{"id":"run_1","eval":"stub","eval_version":3,"commit":"abc1234",
                    "graders":[{"name":"no-error","version":1,"kind":"FixedRule"},{"name":"meal","version":2,"kind":"Model"}]}}
            """));

        Assert.That((run.Eval, run.Version, run.Commit), Is.EqualTo(("stub", 3, "abc1234")));
        Assert.That(run.Graders, Is.EqualTo(new[] { new GraderRow("no-error", 1, "FixedRule"), new GraderRow("meal", 2, "Model") }));
    }

    // F18 step 6a.3: a human review's question rides on its grader row; the other graders have none.
    [Test]
    public void AHumanReviewsQuestion_IsReadWithItsGrader_AndAnotherGradersHasNone()
    {
        var run = RunGradersReader.Read(Json("""
            {"run":{"id":"run_1","eval":"puls-cook","eval_version":9,"commit":"abc",
                    "graders":[{"name":"no-error","version":1,"kind":"FixedRule","question":null},{"name":"meal.human","version":1,"kind":"Human","question":"Is this a meal?"}]}}
            """));

        Assert.That(run.Graders, Is.EqualTo(new[] { new GraderRow("no-error", 1, "FixedRule"), new GraderRow("meal.human", 1, "Human", "Is this a meal?") }));
    }

    [Test]
    public void ARunFromBeforeStep2a_HasNullGraders()
    {
        var run = RunGradersReader.Read(Json("""{"run":{"id":"run_0","eval":null,"eval_version":null,"commit":null,"graders":null}}"""));

        Assert.That(run, Is.EqualTo(new RunGraders(null, null, null, null)));
    }

    [Test]
    public void ARunThatSendsNoSuchFields_HasNullGraders()
    {
        // The field left out altogether (an older API), not only sent as null.
        var run = RunGradersReader.Read(Json("""{"run":{"id":"run_0"}}"""));

        Assert.That(run.Graders, Is.Null);
    }

    [Test]
    public void AConversationsGradesComeInTheStoredOrder_WithItsOutcome()
    {
        var grades = ConversationGradesReader.Read(Json("""
            {"mode":"puls-cook","task":"bobotie",
             "grades":[{"grader":"within-time","version":1,"score":1,"outcome":"Pass","reason":"fine"},
                       {"grader":"no-error","version":1,"score":0,"outcome":"fail","reason":"turn 2 raised an error"}],
             "outcome":"Fail"}
            """));

        Assert.That(grades.Grades, Is.EqualTo(new[]
        {
            new GradeRow("within-time", 1, 1, GradeOutcome.Pass, "fine"),
            new GradeRow("no-error", 1, 0, GradeOutcome.Fail, "turn 2 raised an error"),
        }));
        Assert.That(grades.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    // F18 step 6a.3: the people's grades of a conversation ride on its row (`human_grades`); a conversation nobody graded has none, and an older API sends the field not at all.
    [Test]
    public void APeoplesGradesAreReadWithTheConversation_AndNoFieldIsNoGrades()
    {
        var graded = ConversationGradesReader.Read(Json("""
            {"mode":"puls-cook","task":"bobotie","grades":[],"outcome":"Pass",
             "human_grades":[{"grader":"meal-for-people.human","model_grader":"meal-for-people","verdict":"pass","note":"feeds four","reviewer":"Louis","at":"2026-10-26T09:00:00+00:00","latest":true}]}
            """));
        var older = ConversationGradesReader.Read(Json("""{"mode":"puls-cook","task":"bobotie","grades":[],"outcome":"Pass"}"""));

        Assert.That(graded.Human, Is.EqualTo(new[]
        {
            new HumanGradeRow("meal-for-people.human", "meal-for-people", "pass", "feeds four", "Louis", new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero)),
        }));
        Assert.That(older.Human, Is.Empty);
    }

    [Test]
    public void AGradeWithNoScoreOrVersion_ReadsNull_Never0()
    {
        // ADR 0007: a missing number is unknown; a grader whose row carries no version is unversioned, not version 0.
        var grades = ConversationGradesReader.Read(Json("""{"grades":[{"grader":"meal","outcome":"Unknown","reason":"pending"}],"outcome":"Unknown"}"""));
        var run = RunGradersReader.Read(Json("""{"run":{"graders":[{"name":"meal","kind":"Human"}]}}"""));

        Assert.That(grades.Grades!.Single(), Is.EqualTo(new GradeRow("meal", null, null, GradeOutcome.Unknown, "pending")));
        Assert.That(run.Graders!.Single(), Is.EqualTo(new GraderRow("meal", null, "Human")));
    }

    [Test]
    public void AConversationFromBeforeStep2a_HasNullGradesAndOutcome()
    {
        var grades = ConversationGradesReader.Read(Json("""{"mode":"puls-cook","task":"bobotie","grades":null,"outcome":null}"""));

        Assert.That(grades, Is.EqualTo(ConversationGrades.None));
    }
}
