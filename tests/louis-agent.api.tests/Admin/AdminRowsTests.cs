using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Tests.Admin;

/// <summary>
/// The admin rows show what a run was: its eval, version, commit and graders, and each conversation's grades and
/// outcome, in the order they were stored, with enums spelt the way the protocol and the store spell them. An old run
/// that never recorded these sends null, never [] or 0, so "unknown" can't pass for "none" (ADR 0007). Written with
/// the admin wire's options, as the endpoints write them.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public class AdminRowsTests
{
    /// <summary>When the stored run started; the rows only pass it through.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A row as the endpoint writes it.</summary>
    private static JsonElement Json(object row) => JsonSerializer.SerializeToElement(row, AdminJson.SnakeCase);

    /// <summary>A stored run with an eval and its graders, or, for an old run, neither.</summary>
    private static EvalDocuments.Run Run(string? eval, IReadOnlyList<EvalDocuments.Grader>? graders) =>
        new("run_1", "stub", 1, EvalRunStatus.Done, At, Eval: eval, EvalVersion: eval is null ? null : 3,
            Commit: eval is null ? null : "abc1234",
            Graders: graders);

    /// <summary>A stored conversation with these grades and this outcome.</summary>
    private static EvalDocuments.Conversation Conversation(IReadOnlyList<EvalDocuments.Grade>? grades,
        TrialOutcome? outcome) =>
        new(new ConversationKey(SampleMode.Beta, 1, "bobotie"), 0, SampleRole.Alpha, null, 0, 1.5, grades, outcome);

    /// <summary>A run's eval, version, commit and graders come through, the graders in their order.</summary>
    [Test]
    public void ARunWithAnEvalAndTwoGraders_GivesThemInOrder()
    {
        var row = Json(StoredRunRow.Of(Run("stub",
            [new("no-error", 1, GraderKind.FixedRule), new("meal", 2, GraderKind.Model)])));

        Assert.That(row.GetProperty("eval").GetString(), Is.EqualTo("stub"));
        Assert.That(row.GetProperty("eval_version").GetInt32(), Is.EqualTo(3));
        Assert.That(row.GetProperty("commit").GetString(), Is.EqualTo("abc1234"));
        var graders = row.GetProperty("graders").EnumerateArray()
            .Select(g => (g.GetProperty("name").GetString(), g.GetProperty("version").GetInt32(),
                g.GetProperty("kind").GetString())).ToList();
        Assert.That(graders, Is.EqualTo(new[] { ("no-error", 1, "fixed-rule"), ("meal", 2, "model") }));
    }

    /// <summary>A human review's question rides on its grader's row; another grader has none.</summary>
    [Test]
    public void AHumanReviewsQuestion_RidesOnItsGraderRow_AndAnotherGradersHasNone()
    {
        var row = Json(StoredRunRow.Of(Run("stub",
        [
            new("no-error", 1, GraderKind.FixedRule),
            new("meal.human", 1, GraderKind.Human, "Is this a meal?")
        ])));

        var graders = row.GetProperty("graders").EnumerateArray().ToList();
        Assert.That(graders[0].GetProperty("question").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(graders[1].GetProperty("question").GetString(), Is.EqualTo("Is this a meal?"));
    }

    /// <summary>An old run that never recorded its eval sends null for the eval, version, commit and graders.</summary>
    [Test]
    public void ARunFromBeforeStep2a_GivesNulls()
    {
        var row = Json(StoredRunRow.Of(Run(null, null)));

        Assert.That(row.GetProperty("eval").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(row.GetProperty("eval_version").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(row.GetProperty("commit").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(row.GetProperty("graders").ValueKind, Is.EqualTo(JsonValueKind.Null), "null, never []");
    }

    /// <summary>A conversation's grades come through in order, each with its outcome, and so does its own.</summary>
    [Test]
    public void AConversationWithTwoGrades_GivesThemInOrder_WithTheirOutcomes()
    {
        var grades = new EvalDocuments.Grade[]
        {
            new("within-time", 1, 1, GradeOutcome.Pass, "every turn was answered within 60 s"),
            new("no-error", 1, 0, GradeOutcome.Fail, "turn 2 raised an error"),
        };
        var row = Json(ConversationRow.Of(Conversation(grades, TrialOutcome.Fail), []));

        // Each grade as the five fields the page shows.
        var sent = row.GetProperty("grades").EnumerateArray()
            .Select(g => (g.GetProperty("grader").GetString(), g.GetProperty("version").GetInt32(),
                g.GetProperty("score").GetDouble(),
                g.GetProperty("outcome").GetString(), g.GetProperty("reason").GetString()))
            .ToList();
        Assert.That(sent,
            Is.EqualTo(new[]
            {
                ("within-time", 1, 1.0, "pass", "every turn was answered within 60 s"),
                ("no-error", 1, 0.0, "fail", "turn 2 raised an error"),
            }));
        Assert.That(row.GetProperty("outcome").GetString(), Is.EqualTo("fail"));
    }

    /// <summary>An old conversation that was never graded sends null for its grades and its outcome.</summary>
    [Test]
    public void AConversationFromBeforeStep2a_GivesNulls()
    {
        var row = Json(ConversationRow.Of(Conversation(null, null), []));

        Assert.That(row.GetProperty("grades").ValueKind, Is.EqualTo(JsonValueKind.Null), "null, never []");
        Assert.That(row.GetProperty("outcome").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }
}
