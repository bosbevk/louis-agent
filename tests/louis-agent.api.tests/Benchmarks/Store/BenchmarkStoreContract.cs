using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Store;

/// <summary>
/// F31 step 2: what every benchmark store does, in memory and in MongoDB (step 5c) alike. Writes are upserts by key and
/// reads come back in the run's order, so the recorder (step 3) can write a document again as it grows and the portal
/// reads it as the check ran. Every test makes its own run ids and check name, so the tests share a real database
/// without seeing each other's runs. Times are whole milliseconds in UTC, as MongoDB keeps them.
/// </summary>
public abstract class BenchmarkStoreContract
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    private IBenchmarkStore _store = null!;
    private string _check = "";

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract IBenchmarkStore CreateStore();

    [SetUp]
    public void SetUpStore()
    {
        _store = CreateStore();
        _check = "check-" + Guid.NewGuid().ToString("N")[..8];
    }

    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    private Run NewRun(string id, DateTimeOffset startedAt, BenchmarkRunStatus status = BenchmarkRunStatus.Running) =>
        new(id, _check, 2, status, startedAt);

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static Conversation NewConversation(string task, int order, string? role = "puls-cook", string mode = "puls-cook", int repeat = 1) =>
        new(new ConversationKey(mode, repeat, task), order, role, Json("""{"routed_right":true,"priced_ratio":0.75}"""), 0, 12.5);

    private static Turn NewTurn(string task, int n, string? answer = "Hier is…", string mode = "puls-cook", int repeat = 1) =>
        new(new TurnKey(mode, repeat, task, n), "sess_" + n, "Vraag " + n, answer, "puls-cook", null, 1, 7, ["www.checkers.co.za"], 41.3, null);

    private static StreamEvent Event(string type, string text) =>
        new(type, Json(JsonSerializer.Serialize(new { delta = new { text } })), T0.AddSeconds(text.Length));

    [Test]
    public async Task ARun_ReadsBackAsItWasWritten()
    {
        string id = NewId();
        var run = NewRun(id, T0) with
        {
            Status = BenchmarkRunStatus.Done,
            EndedAt = T0.AddMinutes(9),
            ExitCode = 0,
            Settings = new Dictionary<string, string> { ["model"] = "claude-haiku-5-5", ["commit"] = "45a98b3" },
            Modes = [new Mode("puls-cook", 1, ["specialists", "web"], "puls/voice-af", null)],
            Total = new Total(0.4217m, "USD", 812_345, 61),
            Log = ["puls-cook bobotie role=puls-cook", "done"],
            StoreError = null,
        };

        await _store.SaveRunAsync(run);
        var read = await _store.GetRunAsync(id);

        Assert.That(read, Is.Not.Null);
        Assert.That((read!.Id, read.Check, read.Repeat, read.Status), Is.EqualTo((id, _check, 2, BenchmarkRunStatus.Done)));
        Assert.That((read.StartedAt, read.EndedAt, read.ExitCode), Is.EqualTo((T0, (DateTimeOffset?)T0.AddMinutes(9), (int?)0)));
        Assert.That(read.Settings, Is.EquivalentTo(run.Settings));
        Assert.That(read.Modes!.Single() is { Name: "puls-cook", Repeat: 1, Role: "puls/voice-af", Llm: null });
        Assert.That(read.Modes!.Single().Toolsets, Is.EqualTo(new[] { "specialists", "web" }));
        Assert.That(read.Total, Is.EqualTo(new Total(0.4217m, "USD", 812_345, 61)));
        Assert.That(read.Log, Is.EqualTo(run.Log));
        Assert.That(read.StoreError, Is.Null);
    }

    [Test]
    public void ARunStoredBeforeCL14_ReadsItsModesPersonalityAsTheRole()
    {
        // CL14: runs recorded before CL14 keep "personality" on their modes in MongoDB; they're never rewritten, so read both.
        var run = new Run("r_old", _check, 1, BenchmarkRunStatus.Done, T0, T0, 0, null,
            [new Mode("puls-cook", 1, ["web"], "puls/voice-af", null)], null, null, null);
        var stored = MongoBenchmarkStore.ToBson(run);
        var mode = stored["modes"][0].AsBsonDocument;
        mode.Remove("role");
        mode["personality"] = "puls/voice-af";

        Assert.That(MongoBenchmarkStore.RunOf(stored).Modes!.Single().Role, Is.EqualTo("puls/voice-af"));
    }

    [Test]
    public async Task SavingARunAgain_ReplacesIt()
    {
        string id = NewId();
        await _store.SaveRunAsync(NewRun(id, T0));
        await _store.SaveRunAsync(NewRun(id, T0, BenchmarkRunStatus.Failed) with { EndedAt = T0.AddMinutes(1), ExitCode = 2, StoreError = "store down" });

        var read = await _store.GetRunAsync(id);

        Assert.That((read!.Status, read.ExitCode, read.StoreError), Is.EqualTo((BenchmarkRunStatus.Failed, (int?)2, "store down")));
        Assert.That((await _store.ListRunsAsync(_check)).Count, Is.EqualTo(1));
    }

    [Test]
    public async Task AnUnknownRun_IsNull() =>
        Assert.That(await _store.GetRunAsync(NewId()), Is.Null);

    [Test]
    public async Task TheRuns_AreListedNewestFirst_ForOneCheck_UpToTheLimit()
    {
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
            Models = [new ModelUse("cook", BenchmarkModelLevel.Agent, "puls-cook", "claude-haiku-5-5")],
        };

        await _store.SaveRunAsync(run);
        var read = await _store.GetRunAsync(id);

        Assert.That((read!.Eval, read.EvalVersion, read.Commit), Is.EqualTo(("cook", (int?)2, "abc123")));
        Assert.That(read.Graders, Is.EqualTo(run.Graders));
        Assert.That(read.Models, Is.EqualTo(run.Models));
    }

    [Test]
    public async Task AConversation_CarriesItsGradesAndOutcome()
    {
        string run = NewId();
        var grades = new[] { new Grade("priced-with-source", 1, 1, BenchmarkOutcome.Pass, "2 of 2 priced with a source") };
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1) with { Grades = grades, Outcome = BenchmarkOutcome.Pass });

        var read = (await _store.ConversationsAsync(run)).Single();

        Assert.That(read.Outcome, Is.EqualTo(BenchmarkOutcome.Pass));
        Assert.That(read.Grades, Is.EqualTo(grades));
    }

    [Test]
    public async Task Conversations_ReadBackInTheirOrder_AndASecondSaveReplacesOne()
    {
        string run = NewId();
        await _store.SaveConversationAsync(run, NewConversation("cottage-pie", 2));
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1, role: null));
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1, role: "puls-cook"));
        await _store.SaveConversationAsync(NewId(), NewConversation("another-runs", 1));

        var read = await _store.ConversationsAsync(run);

        Assert.That(read.Select(c => c.Key.Task), Is.EqualTo(new[] { "bobotie", "cottage-pie" }));
        Assert.That(read[0].Role, Is.EqualTo("puls-cook"));
        Assert.That(read[0].Scores.GetProperty("priced_ratio").GetDouble(), Is.EqualTo(0.75));
        Assert.That(read[0].Scores.GetProperty("routed_right").GetBoolean(), Is.True);
        Assert.That((read[0].Errors, read[0].Seconds), Is.EqualTo((0, 12.5)));
    }

    [Test]
    public async Task AConversationsKey_IsItsModeRepeatAndTask()
    {
        string run = NewId();
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 1, mode: "puls-only"));
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 2, mode: "puls-cook"));
        await _store.SaveConversationAsync(run, NewConversation("bobotie", 3, mode: "puls-cook", repeat: 2));

        var read = await _store.ConversationsAsync(run);

        Assert.That(read.Select(c => c.Key), Is.EqualTo(new[]
        {
            new ConversationKey("puls-only", 1, "bobotie"), new ConversationKey("puls-cook", 1, "bobotie"), new ConversationKey("puls-cook", 2, "bobotie"),
        }));
    }

    [Test]
    public async Task Turns_ReadBackByNumber_ForTheirConversationOnly()
    {
        string run = NewId();
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 2), []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1), []);
        await _store.SaveTurnAsync(run, NewTurn("cottage-pie", 1), []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1, repeat: 2), []);

        var read = await _store.TurnsAsync(run, new ConversationKey("puls-cook", 1, "bobotie"));

        Assert.That(read.Select(t => t.Key.Turn), Is.EqualTo(new[] { 1, 2 }));
        var first = read[0];
        Assert.That((first.Session, first.Question, first.Answer, first.Role, first.Continued), Is.EqualTo(("sess_1", "Vraag 1", "Hier is…", "puls-cook", (string?)null)));
        Assert.That((first.PulsCalls, first.SpecialistCalls, first.Seconds, first.Error), Is.EqualTo((1, 7, 41.3, (string?)null)));
        Assert.That(first.Hosts, Is.EqualTo(new[] { "www.checkers.co.za" }));
    }

    [Test]
    public async Task ATurnsEvents_ReadBackApart_InTheOrderTheyCame()
    {
        string run = NewId();
        var key = new TurnKey("puls-cook", 1, "bobotie", 1);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1), [Event("message_start", "a"), Event("content_block_delta", "Hallo"), Event("message_stop", "")]);

        var events = await _store.TurnEventsAsync(run, key);

        Assert.That(events.Select(e => e.Type), Is.EqualTo(new[] { "message_start", "content_block_delta", "message_stop" }));
        Assert.That(events[1].Data.GetProperty("delta").GetProperty("text").GetString(), Is.EqualTo("Hallo"));
        Assert.That(events[1].At, Is.EqualTo(T0.AddSeconds(5)));
    }

    [Test]
    public async Task ATurnSavedAgain_ReplacesItsEvents()
    {
        string run = NewId();
        var key = new TurnKey("puls-cook", 1, "bobotie", 1);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1, answer: null), [Event("message_start", "a")]);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 1), [Event("message_start", "a"), Event("message_stop", "")]);

        Assert.That((await _store.TurnEventsAsync(run, key)).Count, Is.EqualTo(2));
        Assert.That((await _store.TurnsAsync(run, key.Conversation)).Single().Answer, Is.EqualTo("Hier is…"));
    }

    [Test]
    public async Task ATurnsToolCounts_ReadBackWithIt_AndNoneStayNone()
    {
        // Review of PR #55: opening a run read every turn's events to count its tools; the counts are kept on the turn.
        string run = NewId();
        var counted = NewTurn("bobotie", 1) with
        {
            Tools = new BenchmarkTools.Summary(3, 1, [new BenchmarkTools.Failure("WebSearch", "Error: Tavily's limit was reached", 1)]),
        };
        await _store.SaveTurnAsync(run, counted, []);
        await _store.SaveTurnAsync(run, NewTurn("bobotie", 2), []);

        var turns = await _store.TurnsAsync(run, counted.Key.Conversation);

        var tools = turns[0].Tools!;
        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((3, 1)));
        Assert.That(tools.Reasons.Single(), Is.EqualTo(new BenchmarkTools.Failure("WebSearch", "Error: Tavily's limit was reached", 1)));
        Assert.That(turns[1].Tools, Is.Null, "a turn recorded before the counts were kept: unknown, not 0 calls");
    }

    [Test]
    public async Task TheEventsOfAnUnknownTurn_AreEmpty() =>
        Assert.That(await _store.TurnEventsAsync(NewId(), new TurnKey("m", 1, "t", 1)), Is.Empty);

    [Test]
    public async Task Requests_ReadBackByTime_WithUnknownsKeptUnknown()
    {
        string run = NewId();
        var turn = new TurnKey("puls-cook", 1, "bobotie", 1);
        Request At(int seconds, TurnKey? key = null, decimal? cost = 0.01m, long? input = 1200) =>
            new(run, key, T0.AddSeconds(seconds), "agent_turn", "sess_1", null, "claude-haiku-5-5", input, 300, 50_000, 0, cost, 2100);

        await _store.AddRequestsAsync(run, [At(30, turn), At(10, turn)]);
        await _store.AddRequestsAsync(run, [At(20, key: null, cost: null, input: null)]);
        await _store.AddRequestsAsync(NewId(), [At(5)]);

        var read = await _store.RequestsAsync(run);

        Assert.That(read.Select(r => r.At), Is.EqualTo(new[] { T0.AddSeconds(10), T0.AddSeconds(20), T0.AddSeconds(30) }));
        Assert.That(read[0], Is.EqualTo(At(10, turn)));
        Assert.That((read[1].Turn, read[1].Cost, read[1].InputTokens), Is.EqualTo(((TurnKey?)null, (decimal?)null, (long?)null)),
            "an unknown price or count stays unknown, never 0");
    }

    [Test]
    public async Task ClosingOrphans_FailsTheRunsStillRunning_AndLeavesTheFinishedOnes()
    {
        string running = NewId(), done = NewId();
        await _store.SaveRunAsync(NewRun(running, T0));
        await _store.SaveRunAsync(NewRun(done, T0, BenchmarkRunStatus.Done) with { EndedAt = T0.AddMinutes(5), ExitCode = 0 });

        int closed = await _store.CloseOrphansAsync(T0.AddHours(1));

        Assert.That(closed, Is.GreaterThanOrEqualTo(1));
        var orphan = await _store.GetRunAsync(running);
        Assert.That((orphan!.Status, orphan.EndedAt), Is.EqualTo((BenchmarkRunStatus.Failed, (DateTimeOffset?)T0.AddHours(1))));
        var finished = await _store.GetRunAsync(done);
        Assert.That((finished!.Status, finished.EndedAt), Is.EqualTo((BenchmarkRunStatus.Done, (DateTimeOffset?)T0.AddMinutes(5))));
    }

    [Test]
    public async Task DeletingARun_DeletesEverythingItKept()
    {
        string run = NewId(), other = NewId();
        var turn = new TurnKey("puls-cook", 1, "bobotie", 1);
        foreach (string id in new[] { run, other })
        {
            await _store.SaveRunAsync(NewRun(id, T0, BenchmarkRunStatus.Done));
            await _store.SaveConversationAsync(id, NewConversation("bobotie", 1));
            await _store.SaveTurnAsync(id, NewTurn("bobotie", 1), [Event("message_start", "a")]);
            await _store.AddRequestsAsync(id, [new Request(id, turn, T0, "agent_turn", "sess_1", null, "m", 1, 1, 0, 0, 0.01m, 10)]);
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

/// <summary>The contract against the in-memory store: always, on every machine and on CI.</summary>
public class InMemoryBenchmarkStoreTests : BenchmarkStoreContract
{
    protected override IBenchmarkStore CreateStore() => new InMemoryBenchmarkStore();
}

/// <summary>
/// The contract against MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set in the test's environment: on Louis's machine
/// the Puls stack's <c>puls-mongo</c> at 127.0.0.1 (set in the shell, not read from <c>.env.secrets</c>), on CI its Mongo
/// service. Without it the fixture says it's inconclusive rather than passing. Its database is
/// <c>puls_benchmarks_test</c> (or <c>BENCHMARKS_MONGO_DATABASE</c>), dropped when the fixture ends.
/// </summary>
public class MongoBenchmarkStoreTests : BenchmarkStoreContract
{
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private static string Database =>
        Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_DATABASE") is { Length: > 0 } name ? name : "puls_benchmarks_test";

    [OneTimeSetUp]
    public void NeedsMongo() =>
        Assume.That(Url, Is.Not.Null.And.Not.Empty, "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        var store = new MongoBenchmarkStore(Url, Database);
        await store.Database.Client.DropDatabaseAsync(Database);
    }

    protected override IBenchmarkStore CreateStore() => new MongoBenchmarkStore(Url!, Database);
}
