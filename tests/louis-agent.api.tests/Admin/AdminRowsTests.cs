using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Admin;

// The admin rows carry the standard records (F18 step 4.5b): the run's eval, version, commit and graders, and a conversation's grades and
// outcome, each in the stored order, with enums spelt as the protocol and the store spell them; a run from before step 2a sends null,
// never [] or 0 (ADR 0007). Serialized as Results.Json does with no options of its own: the web defaults. Written first: the rows had
// no such fields on the red run.
public class AdminRowsTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    private static JsonElement Json(object row) => JsonSerializer.SerializeToElement(row, Web);

    private static BenchmarkDocuments.Run Run(string? eval, IReadOnlyList<BenchmarkDocuments.Grader>? graders) =>
        new("run_1", "stub", 1, BenchmarkRunStatus.Done, At, Eval: eval, EvalVersion: eval is null ? null : 3, Commit: eval is null ? null : "abc1234",
            Graders: graders);

    private static BenchmarkDocuments.Conversation Conversation(IReadOnlyList<BenchmarkDocuments.Grade>? grades, BenchmarkOutcome? outcome) =>
        new(new ConversationKey(SampleMode.Beta, 1, "bobotie"), 0, "puls-cook", JsonDocument.Parse("{}").RootElement, 0, 1.5, grades, outcome);

    [Test]
    public void ARunWithAnEvalAndTwoGraders_GivesThemInOrder()
    {
        var row = Json(AdminRows.RunRow(Run("stub", [new("no-error", 1, BenchmarkGraderKind.FixedRule), new("meal", 2, BenchmarkGraderKind.Model)])));

        Assert.That(row.GetProperty("eval").GetString(), Is.EqualTo("stub"));
        Assert.That(row.GetProperty("eval_version").GetInt32(), Is.EqualTo(3));
        Assert.That(row.GetProperty("commit").GetString(), Is.EqualTo("abc1234"));
        var graders = row.GetProperty("graders").EnumerateArray()
            .Select(g => (g.GetProperty("name").GetString(), g.GetProperty("version").GetInt32(), g.GetProperty("kind").GetString())).ToList();
        Assert.That(graders, Is.EqualTo(new[] { ("no-error", 1, "FixedRule"), ("meal", 2, "Model") }));
    }

    [Test]
    public void AHumanReviewsQuestion_RidesOnItsGraderRow_AndAnotherGradersHasNone()
    {
        var row = Json(AdminRows.RunRow(Run("stub", [new("no-error", 1, BenchmarkGraderKind.FixedRule), new("meal.human", 1, BenchmarkGraderKind.Human, "Is this a meal?")])));

        var graders = row.GetProperty("graders").EnumerateArray().ToList();
        Assert.That(graders[0].GetProperty("question").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(graders[1].GetProperty("question").GetString(), Is.EqualTo("Is this a meal?"));
    }

    [Test]
    public void ARunFromBeforeStep2a_GivesNulls()
    {
        var row = Json(AdminRows.RunRow(Run(null, null)));

        Assert.That(row.GetProperty("eval").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(row.GetProperty("eval_version").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(row.GetProperty("commit").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(row.GetProperty("graders").ValueKind, Is.EqualTo(JsonValueKind.Null), "null, never []");
    }

    [Test]
    public void AConversationWithTwoGrades_GivesThemInOrder_WithTheirOutcomes()
    {
        var grades = new BenchmarkDocuments.Grade[]
        {
            new("within-time", 1, 1, BenchmarkOutcome.Pass, "every turn was answered within 60 s"),
            new("no-error", 1, 0, BenchmarkOutcome.Fail, "turn 2 raised an error"),
        };
        var row = Json(AdminRows.ConversationRow(Conversation(grades, BenchmarkOutcome.Fail)));

        var sent = row.GetProperty("grades").EnumerateArray()
            .Select(g => (g.GetProperty("grader").GetString(), g.GetProperty("version").GetInt32(), g.GetProperty("score").GetDouble(),
                g.GetProperty("outcome").GetString(), g.GetProperty("reason").GetString()))
            .ToList();
        Assert.That(sent, Is.EqualTo(new[]
        {
            ("within-time", 1, 1.0, "Pass", "every turn was answered within 60 s"),
            ("no-error", 1, 0.0, "Fail", "turn 2 raised an error"),
        }));
        Assert.That(row.GetProperty("outcome").GetString(), Is.EqualTo("Fail"));
    }

    [Test]
    public void AConversationFromBeforeStep2a_GivesNulls()
    {
        var row = Json(AdminRows.ConversationRow(Conversation(null, null)));

        Assert.That(row.GetProperty("grades").ValueKind, Is.EqualTo(JsonValueKind.Null), "null, never []");
        Assert.That(row.GetProperty("outcome").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }
}
