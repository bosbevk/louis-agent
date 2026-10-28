using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Runs;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of runs keeps on a run beyond its first fields, in memory and in MongoDB alike: a review's
/// question, the run it re-scored, a cost of a known zero, the roll-up's slices, each agent's prompts and the protocol
/// version it was recorded from. A run stored before a field existed reads it as null. It sits apart from
/// <see cref="RunStoreContract"/> to keep both short.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store and the Mongo store are held to the same rules.
/// </remarks>
public abstract class RunFieldsContract
{
    /// <summary>The moment the tests start from.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The store under test.</summary>
    private IRunStore _store = null!;

    /// <summary>A store to test; each test gets a fresh one.</summary>
    protected abstract IRunStore CreateStore();

    /// <summary>A fresh store before every test.</summary>
    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>A run with only its first fields, still going.</summary>
    private static Run NewRun(string id) => new(id, "check-" + id[..6], 2, EvalRunStatus.Running, T0);

    /// <summary>The question of a human review is kept with the run's graders, in both stores.</summary>
    [Test]
    public async Task AHumanReviewsQuestion_ReadsBackWithTheRunsGraders()
    {
        string id = NewId();
        var graders = new[]
        {
            new Grader("routed-to", 1, GraderKind.FixedRule),
            new Grader("meal-for-people.human", 1, GraderKind.Human, "Is this a meal for the people asked for?")
        };
        await _store.SaveRunAsync(NewRun(id) with { Graders = graders });

        var read = (await _store.GetRunAsync(id))!;

        Assert.That(read.Graders, Is.EqualTo(graders));
        Assert.That(read.Graders![0].Question, Is.Null);
    }

    /// <summary>A re-score names the run it graded again; an ordinary run names none.</summary>
    [Test]
    public async Task ARunsRescoreOf_ReadsBackAsItWasWritten_AndARunBeforeItHasNone()
    {
        string rescored = NewId(), ordinary = NewId();

        await _store.SaveRunAsync(NewRun(rescored) with { RescoreOf = "old123456789" });
        await _store.SaveRunAsync(NewRun(ordinary));

        Assert.That((await _store.GetRunAsync(rescored))!.RescoreOf, Is.EqualTo("old123456789"));
        Assert.That((await _store.GetRunAsync(ordinary))!.RescoreOf, Is.Null);
    }

    /// <summary>A re-score asked nothing, so its cost is known to be 0; null would mean "unknown" (ADR 0007).</summary>
    [Test]
    public async Task ATotalOfZero_ReadsBackAsZero_NotAsUnknown()
    {
        string id = NewId();

        await _store.SaveRunAsync(NewRun(id) with { Total = new Total(0m, "USD", 0, 0) });

        Assert.That((await _store.GetRunAsync(id))!.Total, Is.EqualTo(new Total(0m, "USD", 0, 0)));
    }

    /// <summary>
    /// The roll-up's slices read back field by field, a grader's mean score too (null when it gave none); a run stored
    /// before them has none.
    /// </summary>
    [Test]
    public async Task ARunsSlices_ReadBackAsTheRunnerWroteThem_AndARunBeforeThemHasNone()
    {
        string rolled = NewId(), older = NewId();
        var slices = new[]
        {
            new EvalSlice(SampleMode.Beta, "overall", 8, 6, 1, 1, 0, ["a", "b"], false, 0.75, 1.0, 0.5,
                [
                    new GraderCount("routed-to", GraderKind.FixedRule, 7, 1, 0, 0.875),
                    new GraderCount("meal-for-people.human", GraderKind.Human, 0, 0, 0, null)
                ],
                [new KindCount(GraderKind.FixedRule, 7, 1, 0)]),
            new EvalSlice(SampleMode.Beta, "language:af", 3, 3, 0, 0, 0, ["a"], true, null, null, null, [], []),
        };

        await _store.SaveRunAsync(NewRun(rolled) with { Slices = slices });
        await _store.SaveRunAsync(NewRun(older));

        // A record holds its lists by reference, so the slices are compared by what they say.
        Assert.That(JsonSerializer.Serialize((await _store.GetRunAsync(rolled))!.Slices),
            Is.EqualTo(JsonSerializer.Serialize(slices)), "every field, a rate left out still null");
        Assert.That((await _store.GetRunAsync(older))!.Slices, Is.Null, "never an empty list, never zeros");
    }

    /// <summary>
    /// Each agent's prompts read back as the recorder wrote them, a mode left out still none; a run stored before they
    /// were kept has none, so the trend never marks it.
    /// </summary>
    [Test]
    public async Task ARunsAgentPrompts_ReadBackAsWritten_AndARunBeforeThemHasNone()
    {
        string hashed = NewId(), older = NewId();
        var prompts = new[]
        {
            new AgentPrompt(SampleMode.Beta, "main", ["a", "b"]),
            new AgentPrompt(SampleMode.Beta, "sub-session:puls-cook", ["c"]),
            new AgentPrompt(null, "main (grading)", ["j"]),
        };

        await _store.SaveRunAsync(NewRun(hashed) with { Prompts = prompts });
        await _store.SaveRunAsync(NewRun(older));

        // A record holds its lists by reference, so the prompts are compared by what they say.
        Assert.That(JsonSerializer.Serialize((await _store.GetRunAsync(hashed))!.Prompts),
            Is.EqualTo(JsonSerializer.Serialize(prompts)));
        Assert.That((await _store.GetRunAsync(older))!.Prompts, Is.Null, "unknown, never an empty list");
    }

    /// <summary>
    /// The protocol version a run was recorded from reads back as written; a run stored before it was kept has none,
    /// which is how a store tells an older run (F18 step 7c.3).
    /// </summary>
    [Test]
    public async Task ARunsProtocolVersion_ReadsBackAsWritten_AndARunBeforeItHasNone()
    {
        string tagged = NewId(), older = NewId();

        await _store.SaveRunAsync(NewRun(tagged) with { ProtocolVersion = 2 });
        await _store.SaveRunAsync(NewRun(older));

        Assert.That((await _store.GetRunAsync(tagged))!.ProtocolVersion, Is.EqualTo(2));
        Assert.That((await _store.GetRunAsync(older))!.ProtocolVersion, Is.Null);
    }
}
