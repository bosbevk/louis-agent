using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of eval runs does, in memory and in MongoDB alike. A write replaces whatever has the same key, and
/// reads come back in the run's order, so the recorder can write a document again as it grows and the page can read
/// the run while it goes. Every test makes its own run ids and check name, so the tests can share a real database
/// without seeing each other's runs. Times are whole milliseconds in UTC, as MongoDB keeps them.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store the unit tests use and the Mongo store can't drift apart.
/// </remarks>
public abstract class BenchmarkStoreContract
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    private IBenchmarkStore _store = null!;

    /// <summary>This test's own check name, so its runs never show up in another test's list.</summary>
    private string _check = "";

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract IBenchmarkStore CreateStore();

    /// <summary>A fresh store and a check name of its own before every test.</summary>
    [SetUp]
    public void SetUpStore()
    {
        _store = CreateStore();
        _check = "check-" + Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>A run of this test's check with only its first fields, still going unless told otherwise.</summary>
    private Run NewRun(string id, DateTimeOffset startedAt, BenchmarkRunStatus status = BenchmarkRunStatus.Running) =>
        new(id, _check, 2, status, startedAt);

    /// <summary>Parses a bit of JSON into a value we can keep.</summary>
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    /// <summary>A scored conversation of one task, by default the beta mode's first repeat.</summary>
    private static Conversation NewConversation(string task, int order, string? role = SampleRole.Alpha,
        string mode = SampleMode.Beta, int repeat = 1) =>
        new(new ConversationKey(mode, repeat, task), order, role, Json("""{"routed_right":true,"priced_ratio":0.75}"""),
            0, 12.5);

    /// <summary>Turn number <paramref name="n"/> of a task's conversation, answered unless told otherwise.</summary>
    private static Turn NewTurn(string task, int n, string? answer = "Hier is…", string mode = SampleMode.Beta,
        int repeat = 1) =>
        new(new TurnKey(mode, repeat, task, n), "sess_" + n, "Vraag " + n, answer, SampleRole.Alpha, null, 1, 7,
            ["www.checkers.co.za"], 41.3, null);

    /// <summary>
    /// One streamed event carrying some text. Its time is the text's length in seconds, so events have different
    /// times.
    /// </summary>
    private static StreamEvent Event(string type, string text) =>
        new(type, Json(JsonSerializer.Serialize(new { delta = new { text } })), T0.AddSeconds(text.Length));

    /// <summary>
    /// The rates the runner wrote on a mode's line are kept on that mode; rates it left out, and a mode from before
    /// rates existed, stay null.
    /// </summary>
    [Test]
    public async Task AModesRates_ReadBackWithTheRun_AndAModeWithNoneReadsBackWithNone()
    {
        string id = NewId();
        var run = NewRun(id, T0) with
        {
            Modes =
            [
                new Mode(SampleMode.Beta, 1, [SampleToolset.Alpha], null, null, new ModeRates(1.0, 0.5, 2, 1, 1)),
                new Mode(SampleMode.Beta, 2, [SampleToolset.Alpha], null, null, new ModeRates(null, null, 1, 3, 0)),
                new Mode(SampleMode.Alpha, 1, [SampleToolset.Alpha], null, null),
            ],
        };

        await _store.SaveRunAsync(run);

        var read = (await _store.GetRunAsync(id))!.Modes!;
        Assert.That(read.Select(m => m.Rates),
            Is.EqualTo(new ModeRates?[] { new(1.0, 0.5, 2, 1, 1), new(null, null, 1, 3, 0), null }));
    }

    /// <summary>A finished run comes back with every field as it was written.</summary>
    [Test]
    public async Task ARun_ReadsBackAsItWasWritten()
    {
        string id = NewId();
        var run = NewRun(id, T0) with
        {
            Status = BenchmarkRunStatus.Done,
            EndedAt = T0.AddMinutes(9),
            ExitCode = 0,
            Settings = new Dictionary<string, string>
            {
                ["model"] = "claude-haiku-5-5",
                ["commit"] = "45a98b3"
            },
            Modes = [new Mode(SampleMode.Beta, 1, [SampleToolset.Gamma, SampleToolset.Alpha], "puls/voice-af", null)],
            Total = new Total(0.4217m, "USD", 812_345, 61),
            Log = [SampleRole.Fill("<RoleAlpha> bobotie role=<RoleAlpha>"), "done"],
            StoreError = null,
        };

        await _store.SaveRunAsync(run);
        var read = await _store.GetRunAsync(id);

        Assert.That(read, Is.Not.Null);
        Assert.That((read!.Id, read.Check, read.Repeat, read.Status),
            Is.EqualTo((id, _check, 2, BenchmarkRunStatus.Done)));
        Assert.That((read.StartedAt, read.EndedAt, read.ExitCode),
            Is.EqualTo((T0, (DateTimeOffset?)T0.AddMinutes(9), (int?)0)));
        Assert.That(read.Settings, Is.EquivalentTo(run.Settings));
        Assert.That(read.Modes!.Single() is { Name: SampleMode.Beta, Repeat: 1, Role: "puls/voice-af", Llm: null });
        Assert.That(read.Modes!.Single().Toolsets, Is.EqualTo(new[] { SampleToolset.Gamma, SampleToolset.Alpha }));
        Assert.That(read.Total, Is.EqualTo(new Total(0.4217m, "USD", 812_345, 61)));
        Assert.That(read.Log, Is.EqualTo(run.Log));
        Assert.That(read.StoreError, Is.Null);
    }

    /// <summary>
    /// An old run whose modes still say "personality" (the word before "role") reads it as the role. Old runs are
    /// never rewritten, so the store reads both.
    /// </summary>
    [Test]
    public void ARunStoredBeforeCL14_ReadsItsModesPersonalityAsTheRole()
    {
        // Write a run as Mongo would, then swap its mode's "role" for the old "personality".
        var run = new Run("r_old", _check, 1, BenchmarkRunStatus.Done, T0, T0, 0, null,
            [new Mode(SampleMode.Beta, 1, [SampleToolset.Alpha], "puls/voice-af", null)], null, null, null);
        var stored = RunDocuments.ToBson(run);
        var mode = stored["modes"][0].AsBsonDocument;
        mode.Remove("role");
        mode["personality"] = "puls/voice-af";

        Assert.That(RunDocuments.RunOf(stored).Modes!.Single().Role, Is.EqualTo("puls/voice-af"));
    }

    /// <summary>Saving a run again replaces it rather than adding a second one.</summary>
    [Test]
    public async Task SavingARunAgain_ReplacesIt()
    {
        string id = NewId();
        await _store.SaveRunAsync(NewRun(id, T0));
        await _store.SaveRunAsync(NewRun(id, T0, BenchmarkRunStatus.Failed) with
        {
            EndedAt = T0.AddMinutes(1),
            ExitCode = 2,
            StoreError = "store down"
        });

        var read = await _store.GetRunAsync(id);

        Assert.That((read!.Status, read.ExitCode, read.StoreError),
            Is.EqualTo((BenchmarkRunStatus.Failed, (int?)2, "store down")));
        Assert.That((await _store.ListRunsAsync(_check)).Count, Is.EqualTo(1));
    }

    /// <summary>Asking for a run we don't have gives nothing back, not an error.</summary>
    [Test]
    public async Task AnUnknownRun_IsNull() =>
        Assert.That(await _store.GetRunAsync(NewId()), Is.Null);

    /// <summary>The list shows one check's runs, newest first, and stops at the limit.</summary>
    [Test]
    public async Task TheRuns_AreListedNewestFirst_ForOneCheck_UpToTheLimit()
    {
        // Three runs saved out of order, and one of another check.
        string first = NewId(), second = NewId(), third = NewId();
        await _store.SaveRunAsync(NewRun(second, T0.AddHours(1)));
        await _store.SaveRunAsync(NewRun(first, T0));
        await _store.SaveRunAsync(NewRun(third, T0.AddHours(2)));
        await _store.SaveRunAsync(NewRun(NewId(), T0.AddHours(3)) with { Check = _check + "-other" });

        var all = await _store.ListRunsAsync(_check);
        var two = await _store.ListRunsAsync(_check, limit: 2);

        Assert.That(all.Select(r => r.Id), Is.EqualTo(new[] { third, second, first }));
        Assert.That(two.Select(r => r.Id), Is.EqualTo(new[] { third, second }));
    }

    /// <summary>A run keeps which eval it ran, at what version and commit, with its graders and models.</summary>
    [Test]
    public async Task ARun_CarriesItsEvalGradersAndModels()
    {
        string id = NewId();
        var run = NewRun(id, T0) with
        {
            Eval = "cook",
            EvalVersion = 2,
            Commit = "abc123",
            Graders = [new Grader("priced-with-source", 1, BenchmarkGraderKind.FixedRule)],
            Models = [new ModelUse("cook", BenchmarkModelLevel.Agent, SampleRole.Alpha, "claude-haiku-5-5")],
        };

        await _store.SaveRunAsync(run);
        var read = await _store.GetRunAsync(id);

        Assert.That((read!.Eval, read.EvalVersion, read.Commit), Is.EqualTo(("cook", (int?)2, "abc123")));
        Assert.That(read.Graders, Is.EqualTo(run.Graders));
        Assert.That(read.Models, Is.EqualTo(run.Models));
    }

    /// <summary>A run's split comes back as written, and a run from before splits has none.</summary>
    [TestCase(BenchmarkRunSplit.Dev)]
    [TestCase(BenchmarkRunSplit.HeldOut)]
    [TestCase(BenchmarkRunSplit.All)]
    [TestCase(null)]
    public async Task ARunsSplit_ReadsBackAsItWasWritten_AndARunBeforeItHasNone(BenchmarkRunSplit? split)
    {
        string id = NewId();

        await _store.SaveRunAsync(NewRun(id, T0) with { Split = split });

        Assert.That((await _store.GetRunAsync(id))!.Split, Is.EqualTo(split));
    }

    /// <summary>A conversation keeps its grades and its overall outcome.</summary>
    [Test]
    public async Task AConversation_CarriesItsGradesAndOutcome()
    {
        string run = NewId();
        var grades = new[]
        {
            new Grade("priced-with-source", 1, 1, BenchmarkOutcome.Pass, "2 of 2 priced with a source")
        };
        await _store.SaveConversationAsync(run,
            NewConversation("bobotie", 1) with
            {
                Grades = grades,
                Outcome = BenchmarkOutcome.Pass
            });

        var read = (await _store.ConversationsAsync(run)).Single();

        Assert.That(read.Outcome, Is.EqualTo(BenchmarkOutcome.Pass));
        Assert.That(read.Grades, Is.EqualTo(grades));
    }

    /// <summary>A grade still waiting for a person comes back as pending.</summary>
    [Test]
    public async Task APendingGrade_ReadsBackPending_InBothStores()
    {
        string run = NewId();
        var grades = new[] { new Grade("meal-for-people.human", 1, 0, BenchmarkOutcome.Pending, "waits for a person") };
        await _store.SaveConversationAsync(run,
            NewConversation("bobotie", 1) with
            {
                Grades = grades,
                Outcome = BenchmarkOutcome.Pass
            });

        var read = (await _store.ConversationsAsync(run)).Single();

        Assert.That(read.Grades, Is.EqualTo(grades));
        Assert.That(read.Outcome, Is.EqualTo(BenchmarkOutcome.Pass));
    }

    /// <summary>A judge's grade keeps the judge that gave it; a code grader's grade has no judge model.</summary>
    [Test]
    public async Task AGrade_KeepsTheJudgeThatGaveIt()
    {
        string run = NewId();
        var grades = new[]
        {
            new Grade("meal-for-people", 1, 1, BenchmarkOutcome.Pass, "feeds four", "qwen3:8b", "abc123def456"),
            new Grade("no-error", 1, 1, BenchmarkOutcome.Pass, "ok")
        };
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1) with { Grades = grades });

        var read = (await _store.ConversationsAsync(run)).Single();

        Assert.That(read.Grades, Is.EqualTo(grades));
        Assert.That(read.Grades![1].JudgeModel, Is.Null);
    }

    /// <summary>
    /// A run's conversations come back in their order with their scores, and saving one again replaces it.
    /// </summary>
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
        Assert.That(read[0].Scores.GetProperty("priced_ratio").GetDouble(), Is.EqualTo(0.75));
        Assert.That(read[0].Scores.GetProperty("routed_right").GetBoolean(), Is.True);
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

    /// <summary>A conversation's turns come back by number with every field, and only that conversation's.</summary>
    [Test]
    public async Task Turns_ReadBackByNumber_ForTheirConversationOnly()
    {
        // Two turns of bobotie out of order, plus turns of another task and another repeat.
        string run = NewId();
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 2), []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1), []);
        await _store.SaveTurnAsync(run, NewTurn("cottage-pie", 1), []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1, repeat: 2), []);

        var read = await _store.TurnsAsync(run, new ConversationKey(SampleMode.Beta, 1, "bobotie"));

        Assert.That(read.Select(t => t.Key.Turn), Is.EqualTo(new[] { 1, 2 }));
        var first = read[0];
        Assert.That((first.Session, first.Question, first.Answer, first.Role, first.Continued),
            Is.EqualTo(("sess_1", "Vraag 1", "Hier is…", SampleRole.Alpha, (string?)null)));
        Assert.That((first.PulsCalls, first.SpecialistCalls, first.Seconds, first.Error),
            Is.EqualTo((1, 7, 41.3, (string?)null)));
        Assert.That(first.Hosts, Is.EqualTo(new[] { "www.checkers.co.za" }));
    }

    /// <summary>A turn's failure comes back as written, and a turn that didn't fail has none.</summary>
    [Test]
    public async Task ATurnsFailure_ReadsBackAsItWasWritten_AndATurnWithNoneHasNone()
    {
        string run = NewId();
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1) with { Failure = BenchmarkTurnFailure.ReplayMiss }, []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 2), []);

        var read = await _store.TurnsAsync(run, new ConversationKey(SampleMode.Beta, 1, "bobotie"));

        Assert.That(read.Select(t => t.Failure),
            Is.EqualTo(new BenchmarkTurnFailure?[] { BenchmarkTurnFailure.ReplayMiss, null }));
    }

    /// <summary>A turn's stream events are read on their own, in the order they came, with data and time.</summary>
    [Test]
    public async Task ATurnsEvents_ReadBackApart_InTheOrderTheyCame()
    {
        string run = NewId();
        var key = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1),
            [Event("message_start", "a"), Event("content_block_delta", "Hallo"), Event("message_stop", "")]);

        var events = await _store.TurnEventsAsync(run, key);

        Assert.That(events.Select(e => e.Type),
            Is.EqualTo(new[] { "message_start", "content_block_delta", "message_stop" }));
        Assert.That(events[1].Data.GetProperty("delta").GetProperty("text").GetString(), Is.EqualTo("Hallo"));
        Assert.That(events[1].At, Is.EqualTo(T0.AddSeconds(5)));
    }

    /// <summary>Saving a turn again replaces its events and answer, as the recorder does while it streams.</summary>
    [Test]
    public async Task ATurnSavedAgain_ReplacesItsEvents()
    {
        // First half-done with one event, then finished with two.
        string run = NewId();
        var key = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1, answer: null), [Event("message_start", "a")]);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1),
            [Event("message_start", "a"), Event("message_stop", "")]);

        Assert.That((await _store.TurnEventsAsync(run, key)).Count, Is.EqualTo(2));
        Assert.That((await _store.TurnsAsync(run, key.Conversation)).Single().Answer, Is.EqualTo("Hier is…"));
    }

    /// <summary>
    /// A turn's tool counts are kept on the turn, so opening a run never reads every event to count them. A turn
    /// recorded before the counts were kept has none.
    /// </summary>
    [Test]
    public async Task ATurnsToolCounts_ReadBackWithIt_AndNoneStayNone()
    {
        string run = NewId();
        var counted = NewTurn("bobotie", 1) with
        {
            Tools = new BenchmarkTools.Summary(3, 1,
                [new BenchmarkTools.Failure("WebSearch", "Error: Tavily's limit was reached", 1)]),
        };
        await _store.SaveTurnAsync(run, counted, []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 2), []);

        var turns = await _store.TurnsAsync(run, counted.Key.Conversation);

        var tools = turns[0].Tools!;
        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((3, 1)));
        Assert.That(tools.Reasons.Single(),
            Is.EqualTo(new BenchmarkTools.Failure("WebSearch", "Error: Tavily's limit was reached", 1)));
        Assert.That(turns[1].Tools, Is.Null, "a turn recorded before the counts were kept: unknown, not 0 calls");
    }

    /// <summary>A turn we have nothing for has no events, rather than an error.</summary>
    [Test]
    public async Task TheEventsOfAnUnknownTurn_AreEmpty() =>
        Assert.That(await _store.TurnEventsAsync(NewId(), new TurnKey("m", 1, "t", 1)), Is.Empty);

    /// <summary>
    /// A run's model requests come back by time, only that run's, and a price or count we didn't know stays unknown.
    /// </summary>
    [Test]
    public async Task Requests_ReadBackByTime_WithUnknownsKeptUnknown()
    {
        string run = NewId();
        var turn = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);

        // A request some seconds into the run; leave out the cost or the tokens to make them unknown.
        Request At(int seconds, TurnKey? key = null, decimal? cost = 0.01m, long? input = 1200) =>
            new(run, key, T0.AddSeconds(seconds), "agent_turn", "sess_1", null, "claude-haiku-5-5", input, 300, 50_000,
                0, cost, 2100);

        await _store.AddRequestsAsync(run, [At(30, turn), At(10, turn)]);
        await _store.AddRequestsAsync(run, [At(20, key: null, cost: null, input: null)]);
        await _store.AddRequestsAsync(NewId(), [At(5)]);

        var read = await _store.RequestsAsync(run);

        Assert.That(read.Select(r => r.At),
            Is.EqualTo(new[] { T0.AddSeconds(10), T0.AddSeconds(20), T0.AddSeconds(30) }));
        Assert.That(read[0], Is.EqualTo(At(10, turn)));
        Assert.That((read[1].Turn, read[1].Cost, read[1].InputTokens),
            Is.EqualTo(((TurnKey?)null, (decimal?)null, (long?)null)),
            "an unknown price or count stays unknown, never 0");
    }

    /// <summary>
    /// After a restart, runs still marked as going are closed as failed at that moment; finished runs are left alone.
    /// </summary>
    [Test]
    public async Task ClosingOrphans_FailsTheRunsStillRunning_AndLeavesTheFinishedOnes()
    {
        string running = NewId(), done = NewId();
        await _store.SaveRunAsync(NewRun(running, T0));
        await _store.SaveRunAsync(NewRun(done, T0, BenchmarkRunStatus.Done) with
        {
            EndedAt = T0.AddMinutes(5),
            ExitCode = 0
        });

        int closed = await _store.CloseOrphansAsync(T0.AddHours(1));

        Assert.That(closed, Is.GreaterThanOrEqualTo(1));
        var orphan = await _store.GetRunAsync(running);
        Assert.That((orphan!.Status, orphan.EndedAt),
            Is.EqualTo((BenchmarkRunStatus.Failed, (DateTimeOffset?)T0.AddHours(1))));
        var finished = await _store.GetRunAsync(done);
        Assert.That((finished!.Status, finished.EndedAt),
            Is.EqualTo((BenchmarkRunStatus.Done, (DateTimeOffset?)T0.AddMinutes(5))));
    }

    /// <summary>
    /// Deleting a run takes everything it kept with it (conversations, turns, events, requests) and leaves other runs
    /// alone; deleting it twice finds nothing the second time.
    /// </summary>
    [Test]
    public async Task DeletingARun_DeletesEverythingItKept()
    {
        // Two runs with one of everything each.
        string run = NewId(), other = NewId();
        var turn = new TurnKey(SampleMode.Beta, 1, "bobotie", 1);
        foreach (string id in new[] { run, other })
        {
            await _store.SaveRunAsync(NewRun(id, T0, BenchmarkRunStatus.Done));
            await _store.SaveConversationAsync(id, NewConversation("bobotie", 1));
            await _store.SaveTurnAsync(id, NewTurn("bobotie", 1), [Event("message_start", "a")]);
            await _store.AddRequestsAsync(id,
                [new Request(id, turn, T0, "agent_turn", "sess_1", null, "m", 1, 1, 0, 0, 0.01m, 10)]);
        }

        Assert.That(await _store.DeleteRunAsync(run), Is.True);

        Assert.That(await _store.GetRunAsync(run), Is.Null);
        Assert.That(await _store.ConversationsAsync(run), Is.Empty);
        Assert.That(await _store.TurnsAsync(run, turn.Conversation), Is.Empty);
        Assert.That(await _store.TurnEventsAsync(run, turn), Is.Empty);
        Assert.That(await _store.RequestsAsync(run), Is.Empty);
        Assert.That(await _store.DeleteRunAsync(run), Is.False, "nothing left to delete");
        Assert.That((await _store.ConversationsAsync(other)).Count, Is.EqualTo(1), "another run keeps its own");
    }
}
