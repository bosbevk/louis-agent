using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Identities.Puls.Judges.Tests;

/// <summary>
/// The cook's judge, "is this a meal for the people asked for": an <see cref="IEvaluator"/> that sends the scenario's
/// ask (the people and the town), the answer and its rubric to the judge model, and reads a JSON verdict (<c>meal</c>:
/// true, false or null, and a reason) through a record. A reply it can't read is no verdict, never a pass.
/// </summary>
public class MealForPeopleTests
{
    /// <summary>The ask: four people in Malmesbury.</summary>
    private static readonly CookAsk Ask = new(people: 4, town: "Malmesbury");

    /// <summary>An answer to judge.</summary>
    private const string Answer = "Bobotie vir vier: 'n hoofgereg met 500 g maalvleis, rys en 'n eier-melk-bedekking.";

    /// <summary>
    /// Judges the answer with a judge that gives this reply, and returns the metric and what was sent.
    /// </summary>
    private static async Task<(BooleanMetric Metric, ScriptedJudge Client)> JudgeAsync(string reply,
        CookAsk? ask = null)
    {
        var client = new ScriptedJudge(reply);
        var result = await new MealForPeople("THE PROMPT").EvaluateAsync(
            [new ChatMessage(ChatRole.User, "Wat kan ek kook?")],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer)), new ChatConfiguration(client), [ask ?? Ask]);
        return (result.Metrics.Values.OfType<BooleanMetric>().Single(), client);
    }

    /// <summary>It sends its rubric as the system message, and the ask and the answer as the user's.</summary>
    [Test]
    public async Task ItSendsItsPromptAsTheSystem_AndTheAsk_AndTheAnswerAsTheUser()
    {
        var (_, client) = await JudgeAsync("""{"meal": true, "reason": "feeds four"}""");

        var sent = client.Sent.Single();
        Assert.That(sent.Select(m => m.Role), Is.EqualTo(new[] { ChatRole.System, ChatRole.User }));
        Assert.That(sent[0].Text, Is.EqualTo("THE PROMPT"));
        Assert.That(sent[1].Text,
            Does.Contain("People: 4").And.Not.Contain("Budget").And.Contain("Town: Malmesbury").And.Contain(Answer),
            "6a.4f: the budget is not the judge's to see");
    }

    /// <summary>
    /// The judge is sent the earlier turns too, question and answer, before the answer it judges, and the rubric tells
    /// it a follow-up may rely on them (Louis: the judge failed a follow-up that swapped the meat and gave the list,
    /// since the recipe was in the turn before). A single-turn conversation is sent as before.
    /// </summary>
    [Test]
    public async Task TheEarlierTurns_AreSentBeforeTheAnswer_SoAFollowUpIsJudgedWithWhatItReliesOn()
    {
        var client = new ScriptedJudge("""{"meal": true, "reason": "r"}""");
        ChatMessage[] conversation =
        [
            new(ChatRole.User, "Wat kan ek kook vir vier?"),
            new(ChatRole.Assistant, "Bobotie vir vier: 500 g maalvleis, rys; bak 40 minute."),
            new(ChatRole.User, "Maak dit vir ses.")
        ];

        await new MealForPeople("THE PROMPT").EvaluateAsync(conversation,
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "Vir ses: 750 g maalvleis, meer rys.")),
            new ChatConfiguration(client), [Ask]);

        var sent = client.Sent.Single()[1].Text;
        Assert.That(sent,
            Does.Contain("The conversation so far:").And.Contain("User: Wat kan ek kook vir vier?").And
                .Contain("Assistant: Bobotie vir vier").And.Contain("User: Maak dit vir ses."));
        Assert.That(sent.IndexOf("The conversation so far:", StringComparison.Ordinal),
            Is.LessThan(sent.IndexOf("The answer to judge:", StringComparison.Ordinal)));
        Assert.That(sent, Does.EndWith("Vir ses: 750 g maalvleis, meer rys."));
        Assert.That(CookRubric.Text, Does.Contain("follow-up"),
            "the rubric says a follow-up may rely on the turns before");
    }

    /// <summary>
    /// The question a person is asked to review is the rubric's own title, read from it, so the judge and the person
    /// answer the same words.
    /// </summary>
    [Test]
    public void ThePersonIsAskedTheQuestionTheJudgeIs_ReadFromItsPromptsTitle()
    {
        Assert.That(CookRubric.Question, Is.EqualTo("Is this a meal for the people asked for?"));
    }

    /// <summary>
    /// The calibration compares the judge with a person on the same answers, so the same answer must get the same
    /// verdict: temperature 0 is part of the measure.
    /// </summary>
    [Test]
    public async Task ItAsksAtTemperatureZero()
    {
        var (_, client) = await JudgeAsync("""{"meal": true, "reason": "r"}""");

        Assert.That(client.Options.Single()?.Temperature, Is.EqualTo(0f));
    }

    /// <summary>A yes, a no and a can't-tell are three answers; can't-tell is no value.</summary>
    [Test]
    public async Task ATrueVerdict_AFalseOne_AndACantTell_AreThreeAnswers()
    {
        var yes = (await JudgeAsync("""{"meal": true, "reason": "a main dish for four"}""")).Metric;
        var no = (await JudgeAsync("""{"meal": false, "reason": "a side dish"}""")).Metric;
        var unsure = (await JudgeAsync("""{"meal": null, "reason": "no quantities to tell"}""")).Metric;

        Assert.That((yes.Value, yes.Reason), Is.EqualTo((true, "a main dish for four")));
        Assert.That((no.Value, no.Reason), Is.EqualTo((false, "a side dish")));
        Assert.That((unsure.Value, unsure.Reason), Is.EqualTo(((bool?)null, "no quantities to tell")),
            "can't tell is an answer, and it is no value");
    }

    /// <summary>A verdict in a code fence, or with words around it, is still read.</summary>
    [Test]
    public async Task AVerdictInACodeFence_OrWithWordsAround_IsStillRead()
    {
        var fenced = (await JudgeAsync("```json\n{\"meal\": false, \"reason\": \"a snack\"}\n```")).Metric;
        var wrapped = (await JudgeAsync("Here is my verdict: {\"meal\": true, \"reason\": \"fine\"} Hope that helps."))
            .Metric;

        Assert.That(fenced.Value, Is.False);
        Assert.That(wrapped.Value, Is.True);
    }

    /// <summary>A reply it can't read is no verdict, never a pass, and the grade says why.</summary>
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

    /// <summary>Asked with no people and no town, it still sends what is known, and never the budget.</summary>
    [Test]
    public async Task AskedWithNoPeople_ItStillSendsWhatIsKnown()
    {
        var (_, client) = await JudgeAsync("""{"meal": null, "reason": "r"}""", new CookAsk(people: null, town: null));

        Assert.That(client.Sent.Single()[1].Text,
            Does.Contain("People: not said").And.Not.Contain("Budget").And.Contain("Town: not said"));
    }

    /// <summary>
    /// The rubric is a markdown file beside the cook's eval (ADR 0011); its hash is of its bytes, so a re-score with a
    /// changed rubric is a different measure.
    /// </summary>
    [Test]
    public void TheRubricIsAFileBesideTheEval_AndItsHashIsOfItsBytes()
    {
        var text = CookRubric.Text;

        Assert.That(text,
            Does.Contain("main dish").And.Contain("can't tell").And.Contain("never the prices").And
                .Contain("\"meal\""));
        Assert.That(Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))[..12],
            Is.EqualTo(CookRubric.Hash));
    }

    /// <summary>
    /// What the judge is sent is what the measure is: pinned like what the cook's model is sent.
    /// <c>LOUIS_UPDATE_GOLDEN=1</c> writes it; review the diff before committing.
    /// </summary>
    [Test]
    public async Task WhatTheJudgeIsSent_IsPinned()
    {
        var recorder = new ScriptedJudge("""{"meal": true, "reason": "x"}""");
        await new MealForPeople(CookRubric.Text).EvaluateAsync(
            [new ChatMessage(ChatRole.User, "Wat kan ek kook?")],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer)),
            new ChatConfiguration(recorder), [Ask]);
        var actual = string.Join("\n", recorder.Sent.Single().Select(m => $"[{m.Role.Value}]\n{m.Text}"))
            .ReplaceLineEndings("\n") + "\n";
        var golden = GoldenPath();

        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(golden, actual);
            Assert.Pass($"Wrote {golden}; review its diff before committing.");
        }

        Assert.That(actual, Is.EqualTo(File.ReadAllText(golden).ReplaceLineEndings("\n")));
    }

    /// <summary>The golden of what the judge is sent, beside this file.</summary>
    private static string GoldenPath([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "meal-for-people.sent.txt");

    /// <summary>A judge model that answers with a canned reply and keeps what it was sent.</summary>
    internal sealed class ScriptedJudge(string reply) : IChatClient
    {
        /// <summary>Each request's messages, in order.</summary>
        public List<List<ChatMessage>> Sent { get; } = [];

        /// <summary>Each request's options, in order.</summary>
        public List<ChatOptions?> Options { get; } = [];

        /// <summary>Keeps what it was sent and answers with its reply.</summary>
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Sent.Add(messages.ToList());
            Options.Add(options);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
        }

        /// <summary>Never asked: the judge doesn't stream.</summary>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <summary>No services.</summary>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        /// <summary>Nothing to free.</summary>
        public void Dispose()
        {
        }
    }
}
