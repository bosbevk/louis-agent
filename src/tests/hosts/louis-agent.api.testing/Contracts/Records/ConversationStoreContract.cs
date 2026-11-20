using LouisAgent.Api.Evals.Store.Conversations;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Testing;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of conversations does, in memory and in MongoDB alike. Saving one again replaces it, a run's come
/// back in the order they started, and each keeps what it measured and its grades as they were written. Every test
/// makes its own run ids, so the tests can share a real database.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store the unit tests use and the Mongo store can't drift apart.
/// </remarks>
public abstract class ConversationStoreContract
{
    /// <summary>The store under test.</summary>
    private IConversationStore _store = null!;

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract IConversationStore CreateStore();

    /// <summary>A fresh store before every test.</summary>
    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>What a conversation of two turns measured; its tokens are known, its cost isn't.</summary>
    private static readonly Metrics Measured = new(2, 6, ["www.checkers.co.za"], 2, 1200, 340, 12.5, null);

    /// <summary>An ended conversation of one task, by default the beta mode's first repeat.</summary>
    private static Conversation NewConversation(string task, int order, string? role = SampleRole.Alpha,
        string mode = SampleMode.Beta, int repeat = 1) =>
        new(new ConversationKey(mode, repeat, task), order, role, Measured, 0, 12.5);

    /// <summary>A conversation keeps its grades and its overall outcome.</summary>
    [Test]
    public async Task AConversation_CarriesItsGradesAndOutcome()
    {
        string run = NewId();
        var grades = new[] { new Grade("priced-with-source", 1, 1, GradeOutcome.Pass, "2 of 2 priced with a source") };
        await _store.SaveConversationAsync(run,
            NewConversation("bobotie", 1) with
            {
                Grades = grades,
                Outcome = TrialOutcome.Pass
            });

        var read = (await _store.ConversationsAsync(run)).Single();

        Assert.That(read.Outcome, Is.EqualTo(TrialOutcome.Pass));
        Assert.That(read.Grades, Is.EqualTo(grades));
    }

    /// <summary>A grade still waiting for a person comes back as pending.</summary>
    [Test]
    public async Task APendingGrade_ReadsBackPending_InBothStores()
    {
        string run = NewId();
        var grades = new[] { new Grade("meal-for-people.human", 1, 0, GradeOutcome.Pending, "waits for a person") };
        await _store.SaveConversationAsync(run,
            NewConversation("bobotie", 1) with
            {
                Grades = grades,
                Outcome = TrialOutcome.Pass
            });

        var read = (await _store.ConversationsAsync(run)).Single();

        Assert.That(read.Grades, Is.EqualTo(grades));
        Assert.That(read.Outcome, Is.EqualTo(TrialOutcome.Pass));
    }

    /// <summary>A judge's grade keeps the judge that gave it; a code grader's grade has no judge model.</summary>
    [Test]
    public async Task AGrade_KeepsTheJudgeThatGaveIt()
    {
        string run = NewId();
        var grades = new[]
        {
            new Grade("meal-for-people", 1, 1, GradeOutcome.Pass, "feeds four", "qwen3:8b", "abc123def456"),
            new Grade("no-error", 1, 1, GradeOutcome.Pass, "ok")
        };
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1) with { Grades = grades });

        var read = (await _store.ConversationsAsync(run)).Single();

        Assert.That(read.Grades, Is.EqualTo(grades));
        Assert.That(read.Grades![1].JudgeModel, Is.Null);
    }

    /// <summary>
    /// What a conversation measured reads back field by field, an unknown cost still null; a conversation that hasn't
    /// ended has no metrics.
    /// </summary>
    [Test]
    public async Task WhatAConversationMeasured_ReadsBack_AndOneNotEndedHasNone()
    {
        string run = NewId();
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1));
        await _store.SaveConversationAsync(run, NewConversation("cottage-pie", 2) with { Metrics = null });

        var read = await _store.ConversationsAsync(run);

        var m = read[0].Metrics!;
        Assert.That((m.AgentCalls, m.SpecialistCalls, m.Turns, m.TokensIn, m.TokensOut, m.Seconds, m.Cost),
            Is.EqualTo(((int?)2, (int?)6, (int?)2, (long?)1200, (long?)340, (double?)12.5, (decimal?)null)));
        Assert.That(m.Hosts, Is.EqualTo(new[] { "www.checkers.co.za" }));
        Assert.That(read[1].Metrics, Is.Null);
    }

    /// <summary>A run's conversations come back in their order, and saving one again replaces it.</summary>
    [Test]
    public async Task Conversations_ReadBackInTheirOrder_AndASecondSaveReplacesOne()
    {
        // Saved out of order, bobotie twice, and one conversation of another run.
        string run = NewId();
        await _store.SaveConversationAsync(run, NewConversation("cottage-pie", 2));
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1, role: null));
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1, role: SampleRole.Alpha));
        await _store.SaveConversationAsync(NewId(), NewConversation("another-runs", 1));

        var read = await _store.ConversationsAsync(run);

        Assert.That(read.Select(c => c.Key.Task), Is.EqualTo(new[] { "bobotie", "cottage-pie" }));
        Assert.That(read[0].Role, Is.EqualTo(SampleRole.Alpha));
        Assert.That((read[0].Errors, read[0].Seconds), Is.EqualTo((0, 12.5)));
    }

    /// <summary>The same task in another mode or repeat is another conversation.</summary>
    [Test]
    public async Task AConversationsKey_IsItsModeRepeatAndTask()
    {
        string run = NewId();
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1, mode: SampleMode.Alpha));
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 2, mode: SampleMode.Beta));
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 3, mode: SampleMode.Beta, repeat: 2));

        var read = await _store.ConversationsAsync(run);

        Assert.That(read.Select(c => c.Key),
            Is.EqualTo(new[]
            {
                new ConversationKey(SampleMode.Alpha, 1, "bobotie"),
                new ConversationKey(SampleMode.Beta, 1, "bobotie"),
                new ConversationKey(SampleMode.Beta, 2, "bobotie"),
            }));
    }

    /// <summary>Deleting a run's conversations takes only that run's, and a second delete finds nothing.</summary>
    [Test]
    public async Task DeletingARunsConversations_TakesOnlyThatRuns_AndSaysWhetherThereWereAny()
    {
        string run = NewId(), other = NewId();
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1));
        await _store.SaveConversationAsync(run, NewConversation("cottage-pie", 2));
        await _store.SaveConversationAsync(other, NewConversation("bobotie", 1));

        Assert.That(await _store.DeleteConversationsAsync(run), Is.EqualTo(2));

        Assert.That(await _store.ConversationsAsync(run), Is.Empty);
        Assert.That(await _store.DeleteConversationsAsync(run), Is.Zero, "nothing left to delete");
        Assert.That((await _store.ConversationsAsync(other)).Count, Is.EqualTo(1), "another run keeps its own");
    }
}
