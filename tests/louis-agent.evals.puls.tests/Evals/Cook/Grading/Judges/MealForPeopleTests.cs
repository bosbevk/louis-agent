using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading.Judges;

// F18 step 6.2: the cook's judge, "is this a meal for the people asked for". An IEvaluator that sends the scenario's ask (people, budget, town), the answer and its prompt to the judge
// model, and reads a JSON verdict (meal: true, false or null, and a reason) through a record. A reply it can't read is no verdict, never a pass. Written first: MealForPeople did not exist.
public class MealForPeopleTests
{
    private static readonly CookAsk Ask = new(people: 4, budget: 150m, town: "Malmesbury");
    private const string Answer = "Bobotie vir vier: 'n hoofgereg met 500 g maalvleis, rys en 'n eier-melk-bedekking.";

    private static async Task<(BooleanMetric Metric, ScriptedJudge Client)> JudgeAsync(string reply, CookAsk? ask = null)
    {
        var client = new ScriptedJudge(reply);
        var result = await new MealForPeople("THE PROMPT").EvaluateAsync([new ChatMessage(ChatRole.User, "Wat kan ek kook?")],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer)), new ChatConfiguration(client), [ask ?? Ask]);
        return (result.Metrics.Values.OfType<BooleanMetric>().Single(), client);
    }

    [Test]
    public async Task ItSendsItsPromptAsTheSystem_AndTheAsk_AndTheAnswerAsTheUser()
    {
        var (_, client) = await JudgeAsync("""{"meal": true, "reason": "feeds four"}""");

        var sent = client.Sent.Single();
        Assert.That(sent.Select(m => m.Role), Is.EqualTo(new[] { ChatRole.System, ChatRole.User }));
        Assert.That(sent[0].Text, Is.EqualTo("THE PROMPT"));
        Assert.That(sent[1].Text, Does.Contain("People: 4").And.Contain("Budget: R150").And.Contain("Town: Malmesbury").And.Contain(Answer));
    }

    // F18 step 6a.1: the question a person is asked to review is the judge prompt's own title, read from it, so the judge and the person answer the same words.
    [Test]
    public void ThePersonIsAskedTheQuestionTheJudgeIs_ReadFromItsPromptsTitle()
    {
        Assert.That(MealForPeoplePrompt.Question, Is.EqualTo("Is this a meal for the people asked for?"));
    }

    // The calibration compares the judge with a person on the same answers, so the same answer must get the same verdict: temperature 0 is part of the measure (6.2 review).
    [Test]
    public async Task ItAsksAtTemperatureZero()
    {
        var (_, client) = await JudgeAsync("""{"meal": true, "reason": "r"}""");

        Assert.That(client.Options.Single()?.Temperature, Is.EqualTo(0f));
    }

    [Test]
    public async Task ATrueVerdict_AFalseOne_AndACantTell_AreThreeAnswers()
    {
        var yes = (await JudgeAsync("""{"meal": true, "reason": "a main dish for four"}""")).Metric;
        var no = (await JudgeAsync("""{"meal": false, "reason": "a side dish"}""")).Metric;
        var unsure = (await JudgeAsync("""{"meal": null, "reason": "no quantities to tell"}""")).Metric;

        Assert.That((yes.Value, yes.Reason), Is.EqualTo((true, "a main dish for four")));
        Assert.That((no.Value, no.Reason), Is.EqualTo((false, "a side dish")));
        Assert.That((unsure.Value, unsure.Reason), Is.EqualTo(((bool?)null, "no quantities to tell")), "can't tell is an answer, and it is no value");
    }

    [Test]
    public async Task AVerdictInACodeFence_OrWithWordsAround_IsStillRead()
    {
        var fenced = (await JudgeAsync("```json\n{\"meal\": false, \"reason\": \"a snack\"}\n```")).Metric;
        var wrapped = (await JudgeAsync("Here is my verdict: {\"meal\": true, \"reason\": \"fine\"} Hope that helps.")).Metric;

        Assert.That(fenced.Value, Is.False);
        Assert.That(wrapped.Value, Is.True);
    }

    [TestCase("")]
    [TestCase("I think it is a meal.")]
    [TestCase("""{"meal": "yes", "reason": "x"}""")]
    [TestCase("""{"reason": "no meal key"}""")]
    [TestCase("""{"meal": true""")]
    public async Task AReplyItCantRead_IsNoVerdict_NeverAPass(string reply)
    {
        var metric = (await JudgeAsync(reply)).Metric;

        Assert.That(metric.Value, Is.Null);
        Assert.That(metric.Reason, Is.Not.Empty, "a grade says why");
    }

    [Test]
    public async Task AskedWithNoPeople_ItStillSendsWhatIsKnown()
    {
        var (_, client) = await JudgeAsync("""{"meal": null, "reason": "r"}""", new CookAsk(people: null, budget: null, town: null));

        Assert.That(client.Sent.Single()[1].Text, Does.Contain("People: not said").And.Contain("Budget: none").And.Contain("Town: not said"));
    }

    // The prompt is a markdown file beside the eval (ADR 0011), embedded; its hash is of its bytes, so a re-score with a changed prompt is a different measure.
    [Test]
    public void ThePromptIsTheEmbeddedFile_AndItsHashIsOfItsBytes()
    {
        string text = MealForPeoplePrompt.Text;

        Assert.That(text, Does.Contain("main dish").And.Contain("can't tell").And.Contain("never the prices").And.Contain("\"meal\""));
        Assert.That(MealForPeoplePrompt.Hash, Is.EqualTo(Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))[..12]));
        Assert.That(MealForPeoplePrompt.Hash, Does.Match("^[0-9a-f]{12}$"));
    }

    // What the judge is sent is what the measure is: pinned like what the cook's model is sent. LOUIS_UPDATE_GOLDEN=1 writes it; review the diff before committing.
    [Test]
    public async Task WhatTheJudgeIsSent_IsPinned()
    {
        var recorder = new ScriptedJudge("""{"meal": true, "reason": "x"}""");
        await new MealForPeople(MealForPeoplePrompt.Text).EvaluateAsync([new ChatMessage(ChatRole.User, "Wat kan ek kook?")], new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer)),
            new ChatConfiguration(recorder), [Ask]);
        string actual = string.Join("\n", recorder.Sent.Single().Select(m => $"[{m.Role.Value}]\n{m.Text}")).ReplaceLineEndings("\n") + "\n";
        string golden = GoldenPath();

        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(golden, actual);
            Assert.Pass($"Wrote {golden}; review its diff before committing.");
        }

        Assert.That(actual, Is.EqualTo(File.ReadAllText(golden).ReplaceLineEndings("\n")));
    }

    private static string GoldenPath([CallerFilePath] string here = "") => Path.Combine(Path.GetDirectoryName(here)!, "meal-for-people.sent.txt");

    /// <summary>A judge model that answers with a canned reply and keeps what it was sent.</summary>
    internal sealed class ScriptedJudge(string reply) : IChatClient
    {
        public List<List<ChatMessage>> Sent { get; } = [];
        public List<ChatOptions?> Options { get; } = [];

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Sent.Add(messages.ToList());
            Options.Add(options);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
