using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Tests.Evals.Drafts.Samples;

/// <summary>
/// A conversation of a stored run becomes a sample: its first question is the input and the later ones the
/// follow-ups, in turn order; the place, time zone, expected keys and categories come from the scenario it ran; the
/// person's values go on top. An expected key the scenario doesn't have is refused, since no check reads it (finding O,
/// Louis's pick).
/// </summary>
public class SampleDraftsTests
{
    /// <summary>When the draft was made.</summary>
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The conversation the sample comes from: cook-af, the second repeat.</summary>
    private static readonly ConversationKey CookAf = new(SampleMode.Beta, 2, "cook-af");

    /// <summary>
    /// The scenario the conversation ran: cook-af for four in Stellenbosch, under R250, with a follow-up.
    /// </summary>
    private static readonly SampleBody Scenario = JsonSerializer.Deserialize<SampleBody>(
        """
        {"id":"cook-af","input":[{"role":"user","content":"Wat kan ek vir vier kook?"}],
         "metadata":{"place":"Stellenbosch, Wes-Kaap, Suid-Afrika","time_zone":"Africa/Johannesburg",
         "follow_ups":["Maak dit vir ses."],
         "expected":{"routes_to":"puls-cook","town":"Stellenbosch","serves":[4,6],"budget":250,"shop":"pnp"},
         "categories":["language:af","budget"]}}
        """, SampleBody.FileJson)!;

    /// <summary>A turn of the conversation that asked this question.</summary>
    private static EvalDocuments.Turn Turn(int turn, string question) =>
        new(new TurnKey(CookAf.Mode, CookAf.Repeat, CookAf.Task, turn), "session-1", question, "Antwoord.", null, null,
            0, 0, [], 1.5, null);

    /// <summary>What the person chose: a new id, and nothing else unless the test says.</summary>
    private static SampleChoices Choices(string? expected = null, IReadOnlyList<string>? categories = null,
        string? target = null) =>
        new("cook-af-vyf", expected is null ? null : JsonDocument.Parse(expected).RootElement.Clone(), categories,
            target, "Louis");

    /// <summary>The sample draft made from these turns with these choices.</summary>
    private static SampleDraft Make(SampleChoices choices, params EvalDocuments.Turn[] turns) =>
        SampleDrafts.Of("draft1234567", "eval-a", "identity-a", "run123456789", CookAf, turns, Scenario, choices,
            At);

    /// <summary>The expected values as JSON text, to compare.</summary>
    private static string Expected(SampleDraft draft) => JsonSerializer.Serialize(draft.Sample.Metadata.Expected);

    /// <summary>The first question is the input, as the user's one message.</summary>
    [Test]
    public void TheFirstQuestion_IsTheInput() =>
        Assert.That(Make(Choices(), Turn(1, "Wat kan ek vir vyf kook?")).Sample.Input,
            Is.EqualTo(new[] { new SampleBodyMessage("user", "Wat kan ek vir vyf kook?") }));

    /// <summary>The later questions are the follow-ups, in turn order, whatever order the turns came in.</summary>
    [Test]
    public void TheLaterQuestions_AreTheFollowUps_InTurnOrder()
    {
        var sample = Make(Choices(), Turn(3, "En 'n poeding?"), Turn(1, "Wat kan ek kook?"), Turn(2, "Vir ses?"))
            .Sample;

        Assert.That(sample.Input.Single().Content, Is.EqualTo("Wat kan ek kook?"));
        Assert.That(sample.Metadata.FollowUps, Is.EqualTo(new[] { "Vir ses?", "En 'n poeding?" }));
    }

    /// <summary>
    /// One turn means no follow-ups, even when the scenario had some: the conversation is the sample.
    /// </summary>
    [Test]
    public void OneTurn_MeansNoFollowUps() =>
        Assert.That(Make(Choices(), Turn(1, "Wat kan ek kook?")).Sample.Metadata.FollowUps, Is.Empty);

    /// <summary>
    /// The place, time zone, expected values and categories come from the scenario when the person gives none.
    /// </summary>
    [Test]
    public void ThePlaceTimeZoneExpectedAndCategories_ComeFromTheScenario()
    {
        var draft = Make(Choices(), Turn(1, "Wat kan ek kook?"));

        Assert.That((draft.Sample.Metadata.Place, draft.Sample.Metadata.TimeZone),
            Is.EqualTo(("Stellenbosch, Wes-Kaap, Suid-Afrika", "Africa/Johannesburg")));
        Assert.That(Expected(draft),
            Is.EqualTo("""{"routes_to":"puls-cook","town":"Stellenbosch","serves":[4,6],"budget":250,"shop":"pnp"}"""));
        Assert.That(draft.Sample.Metadata.Categories, Is.EqualTo(new[] { "language:af", "budget" }));
        Assert.That(draft.Sample.Target, Is.Null);
    }

    /// <summary>The person's expected values go on top of the scenario's, key by key; the rest stay.</summary>
    [Test]
    public void ThePersonsExpectedValues_GoOnTop_KeyByKey() =>
        Assert.That(Expected(Make(Choices("""{"serves":5,"budget":null}"""), Turn(1, "Vir vyf?"))),
            Is.EqualTo("""{"routes_to":"puls-cook","town":"Stellenbosch","serves":5,"budget":null,"shop":"pnp"}"""));

    /// <summary>The person's categories and ideal answer replace the scenario's.</summary>
    [Test]
    public void ThePersonsCategoriesAndTarget_AreTaken()
    {
        var sample = Make(Choices(categories: ["language:af", "kind:cook"], target: "Bobotie vir vyf."),
            Turn(1, "Wat kan ek kook?")).Sample;

        Assert.That((sample.Metadata.Categories, sample.Target),
            Is.EqualTo((new[] { "language:af", "kind:cook" }, "Bobotie vir vyf.")));
    }

    /// <summary>
    /// The draft says which eval, identity, run and conversation it's from, its id, who made it and when.
    /// </summary>
    [Test]
    public void TheDraft_SaysWhereItCameFrom_AndWhoMadeIt()
    {
        var draft = Make(Choices(), Turn(1, "Wat kan ek kook?"));

        Assert.That((draft.Id, draft.Eval, draft.Identity, draft.Run, draft.Conversation, draft.Author, draft.At),
            Is.EqualTo(("draft1234567", "eval-a", "identity-a", "run123456789", CookAf, "Louis", At)));
        Assert.That(draft.Sample.Id, Is.EqualTo("cook-af-vyf"));
    }

    /// <summary>An expected key the scenario doesn't have is refused, naming it and the keys it has.</summary>
    [Test]
    public void AnExpectedKeyTheScenarioDoesNotHave_IsRefused()
    {
        var refused = Assert.Throws<ArgumentException>(() =>
            Make(Choices("""{"serve":5}"""), Turn(1, "Vir vyf?")));

        Assert.That(refused!.Message, Does.Contain("serve").And.Contain("serves").And.Contain("routes_to"));
    }

    /// <summary>Expected values that aren't an object are refused.</summary>
    [Test]
    public void ExpectedValuesThatAreNotAnObject_AreRefused() =>
        Assert.Throws<ArgumentException>(() => Make(Choices("[5]"), Turn(1, "Vir vyf?")));

    /// <summary>A conversation with no turns has no question to make a sample of, and is refused.</summary>
    [Test]
    public void AConversationWithNoTurns_IsRefused() =>
        Assert.Throws<ArgumentException>(() => Make(Choices()));

    /// <summary>An id that can't name a scenario file (a capital, a space, a path) is refused, saying so.</summary>
    [TestCase("Cook-af")]
    [TestCase("cook af")]
    [TestCase("../cook")]
    [TestCase("")]
    public void AnIdThatCantNameAFile_IsRefused(string id)
    {
        var refused = Assert.Throws<ArgumentException>(() =>
            Make(Choices() with { SampleId = id }, Turn(1, "Wat kan ek kook?")));

        Assert.That(refused!.Message, Does.Contain("kebab-case"));
    }
}
