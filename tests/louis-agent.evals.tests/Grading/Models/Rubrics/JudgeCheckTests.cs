using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Models;
using LouisAgent.Evals.Grading.Models.Rubrics;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Tests.Datasets;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Models.Rubrics;

/// <summary>
/// The generic <c>judge</c>: a rubric file is the judge's system prompt, it is told only the scenario keys it lists,
/// and it answers <c>{"pass": …, "reason": …}</c>, read into a grade that is never a guessed pass.
/// </summary>
public class JudgeCheckTests
{
    /// <summary>A rubric, with the title a person reviewing the judge is asked as a question.</summary>
    private const string Rubric = "# Judge: is the answer polite\n\nSay whether the answer is polite.\n";

    /// <summary>The entry reads its rubric from the eval's folder and lists its keys for the loader.</summary>
    [Test]
    public void TheEntry_ReadsItsRubric_FromTheEvalsFolder()
    {
        using var folder = new DatasetFolder().Write("rubrics/polite.md", Rubric);

        var check = Build(folder.Root, """{"rubric": "rubrics/polite.md", "ask": ["tone"]}""");

        Assert.That(check, Is.TypeOf<JudgeCheck>());
        Assert.That((check.Name, check.Kind, check.Required), Is.EqualTo(("judge", CheckKind.Model, false)));
        Assert.That(check.ExpectedKeys, Is.EqualTo(new[] { "tone" }));
        Assert.That(((JudgeCheck)check).Rubric, Is.EqualTo(Rubric));
    }

    /// <summary>A rubric that isn't in the folder is refused when the eval is read, naming the file.</summary>
    [Test]
    public void AMissingRubric_IsRefused_NamingTheFile()
    {
        using var folder = new DatasetFolder();

        var refusal = Assert.Throws<InvalidDataException>(() =>
            Build(folder.Root, """{"rubric": "rubrics/gone.md", "ask": []}"""));

        Assert.That(refusal!.Message, Does.Contain("gone.md"));
    }

    /// <summary>
    /// The judge is sent the rubric as its system prompt, then only its own keys, and the answer; its yes is a pass
    /// with its reason, and the grade says which model judged with which rubric.
    /// </summary>
    [Test]
    public async Task TheJudge_IsSentTheRubric_OnlyItsKeys_AndTheAnswer()
    {
        var client = Replying("""{"pass": true, "reason": "Warm and polite."}""");

        var grade = await Judge(client).GradeAsync(
            Trial(Mode, """{"tone": "warm", "budget": 100}""", Turn(1, [Text("Goeie more!")])), CancellationToken.None);

        var (messages, options) = client.Requests.Single();
        Assert.That((grade.Outcome, grade.Reason), Is.EqualTo((GradeOutcome.Pass, "Warm and polite.")));
        Assert.That((grade.Judge?.Model, grade.Judge?.PromptHash.Length), Is.EqualTo(("qwen3:8b", 12)));
        Assert.That((messages[0].Role, messages[0].Text), Is.EqualTo((ChatRole.System, Rubric)));
        Assert.That(messages[1].Text, Does.Contain("tone: warm").And.Contain("Goeie more!").And.Not.Contain("budget"));
        Assert.That(options?.Temperature, Is.EqualTo(0));
    }

    /// <summary>A no fails; a null, or a reply with no verdict in it, is Unknown, never a pass.</summary>
    [TestCase("""{"pass": false, "reason": "Curt."}""", GradeOutcome.Fail)]
    [TestCase("""{"pass": null, "reason": "Can't tell."}""", GradeOutcome.Unknown)]
    [TestCase("I think it is fine.", GradeOutcome.Unknown)]
    public async Task TheVerdict_IsRead(string reply, GradeOutcome outcome)
    {
        var grade = await Judge(Replying(reply)).GradeAsync(
            Trial(Mode, """{"tone": "warm"}""", Turn(1, [Text("Goeie more!")])), CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(outcome));
    }

    /// <summary>A scenario without the judge's keys gives it nothing to judge: no request is made.</summary>
    [Test]
    public async Task AScenarioWithoutItsKeys_HasNothingToJudge()
    {
        var client = Replying("""{"pass": false, "reason": "Curt."}""");

        var grade = await Judge(client).GradeAsync(Trial(Mode, "{}", Turn(1, [Text("Goeie more!")])),
            CancellationToken.None);

        Assert.That((grade.Outcome, client.Requests.Count), Is.EqualTo((GradeOutcome.Pass, 0)));
    }

    /// <summary>A judge model that always gives this reply.</summary>
    private static FakeChatClient Replying(string reply) =>
        new(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));

    /// <summary>The judge over the rubric, asking about <c>tone</c>, through this client.</summary>
    private static JudgeCheck Judge(FakeChatClient client) =>
        new("judge", 1, Rubric, ["tone"], () => new ConfiguredJudge(new ChatConfiguration(client), "qwen3:8b"));

    /// <summary>
    /// Builds the check the way an eval file will: from the general checks' catalogue, in this folder.
    /// </summary>
    private static ICheck Build(string folder, string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("judge")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), new CheckContext(folder, []));
}
