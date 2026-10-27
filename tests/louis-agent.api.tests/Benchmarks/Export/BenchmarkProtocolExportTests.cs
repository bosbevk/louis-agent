using LouisAgent.Testing;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Tests.Endpoints.Routing;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Export;

// F18 step 5.2: a stored run as the protocol's own lines. A run recorded from protocol lines, exported and read back by
// BenchmarkProtocol, is the same run: key for key, in the same order, times aside. The lines are the Python stub's (the runner's
// fixture), so a recorded Python run and an exported one are held to the same reader.
public class BenchmarkProtocolExportTests
{
    private const string Id = "run123456789";
    private const string AdminKey = "admin-secret";

    private static string[] StubLines() =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "python-stub.txt")).Where(l => l.StartsWith('@')).ToArray();

    private static async Task<InMemoryBenchmarkStore> Recorded(IEnumerable<string> lines)
    {
        var store = new InMemoryBenchmarkStore();
        var run = new BenchmarkRun(Id, "cook", 1);
        var recording = new BenchmarkRecorder(store).RecordAsync(run);
        foreach (string line in lines) run.Add(line);
        run.Complete(0);
        await recording;
        return store;
    }

    /// <summary>An event as comparable text: its type and its fields, without the time (the export has no clock of the run's own).</summary>
    private static string Plain(Event e)
    {
        var node = JsonSerializer.SerializeToNode(e, e.GetType())!.AsObject();
        node.Remove("At");
        return e.GetType().Name + node.ToJsonString();
    }

    private static IReadOnlyList<string> Events(IEnumerable<string> lines) =>
        lines.Where(l => l.StartsWith('@')).Select(Parse).Where(e => e is not LogLine).Select(Plain).ToList();

    private static async Task<string> Export(InMemoryBenchmarkStore store) =>
        await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync(Id))!, CancellationToken.None);

    [Test]
    public async Task APythonRunExportedAndReadBack_IsTheSameRun_KeyForKey()
    {
        var store = await Recorded(StubLines());

        string text = await Export(store);

        Assert.That(Events(text.Split('\n', StringSplitOptions.RemoveEmptyEntries)), Is.EqualTo(Events(StubLines())));
    }

    // F18 step 6a.3: an exported run keeps the human review's question on the first line, so a re-score of it still says what was asked.
    [Test]
    public async Task AHumanReviewsQuestion_SurvivesTheExport()
    {
        string At(object e) => "@" + JsonSerializer.Serialize(e);
        var lines = new[]
        {
            At(new { @event = "protocol", version = 1, eval = SampleEval.Beta, eval_version = 9, commit = "abc", graders = new object[] { new { name = "meal-for-people.human", version = 1, kind = "Human", question = "Is this a meal?" } }, models = Array.Empty<object>() }),
            At(new { @event = "mode", mode = SampleMode.Beta, repeat = 1, toolsets = new[] { SampleToolset.Alpha }, role = (string?)null, llm = (string?)null, conversations = 0 }),
            At(new { @event = "mode_done", mode = SampleMode.Beta, repeat = 1 }),
        };
        var store = await Recorded(lines);

        string text = await Export(store);

        var begin = (ProtocolStarted)Parse(text.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]);
        Assert.That(begin.Graders!.Single().Question, Is.EqualTo("Is this a meal?"));
    }

    [Test]
    public async Task AHeldOutRun_KeepsItsSplitOnTheBeginLine_SoAnExportedRunReadsBackAsHeldOut()
    {
        string At(object e) => "@" + JsonSerializer.Serialize(e);
        var lines = new[]
        {
            At(new { @event = "protocol", t = 1700000000.5, version = 1, eval = SampleEval.Beta, eval_version = 3, commit = "abc123", split = "held_out", graders = Array.Empty<object>(), models = Array.Empty<object>() }),
            At(new { @event = "mode", t = 1700000001.0, mode = SampleMode.Beta, repeat = 1, toolsets = new[] { SampleToolset.Alpha }, role = (string?)null, llm = (string?)null, conversations = 0 }),
            At(new { @event = "mode_done", t = 1700000005.0, mode = SampleMode.Beta, repeat = 1 }),
        };
        var store = await Recorded(lines);

        string text = await Export(store);

        var begin = Parse(text.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]);
        Assert.That(text.Split('\n')[0], Does.Contain("\"split\":\"held_out\""));
        Assert.That(((ProtocolStarted)begin).Split, Is.EqualTo(BenchmarkRunSplit.HeldOut));
    }

    [Test]
    public async Task AnEvalRunKeepsItsEvalGradersAndModels_AndEachConversationsGradesAndOutcome()
    {
        string At(object e) => "@" + JsonSerializer.Serialize(e);
        var lines = new[]
        {
            At(new
            {
                @event = "protocol", t = 1700000000.5, version = 1, eval = SampleEval.Beta, eval_version = 2, commit = "abc123",
                graders = new[] { new { name = "cook-no-error", version = 1, kind = "FixedRule" } },
                models = new[] { new { mode = SampleMode.Beta, level = "Agent", role = "puls/voice-af", model = "claude-haiku-5-5" } },
            }),
            At(new { @event = "mode", t = 1700000001.0, mode = SampleMode.Beta, repeat = 1, toolsets = new[] { SampleToolset.Gamma, SampleToolset.Alpha }, role = "puls/voice-af", llm = (string?)null, conversations = 1 }),
            At(new { @event = "question", t = 1700000001.0, mode = SampleMode.Beta, repeat = 1, task = "bobotie", turn = 1, session = "sess_1", text = "Maak bobotie" }),
            At(new { @event = "sse", t = 1700000002.25, mode = SampleMode.Beta, repeat = 1, task = "bobotie", turn = 1, type = "content_block_delta", data = new { delta = new { text = "Hé" } } }),
            At(new
            {
                @event = "turn", t = 1700000003.0, mode = SampleMode.Beta, repeat = 1, task = "bobotie", turn = 1, session = "sess_1", question = "Maak bobotie",
                answer = "Hé", role = "puls-cook", continued = (string?)null, puls_calls = 1, specialist_calls = 6, hosts = new[] { "www.checkers.co.za" },
                seconds = 30.5, error = (string?)null,
            }),
            At(new
            {
                @event = "conversation", t = 1700000004.0, mode = SampleMode.Beta, repeat = 1, task = "bobotie", role = "puls-cook", scores = new { routed_right = true },
                errors = 0, seconds = 30.5, grades = new[] { new { grader = "cook-no-error", version = 1, score = 1.0, outcome = "Pass", reason = "no error" } },
                outcome = "Pass",
            }),
            At(new { @event = "mode_done", t = 1700000005.0, mode = SampleMode.Beta, repeat = 1 }),
        };
        var store = await Recorded(lines);

        string text = await Export(store);

        Assert.That(Events(text.Split('\n', StringSplitOptions.RemoveEmptyEntries)), Is.EqualTo(Events(lines)));
        Assert.That(text.All(c => c < 128), Is.True, "ASCII only, as the harness writes it: the é is escaped");
    }

    // F18 step 6.1b: the judge of a grade survives the export, as the rest of the grade does.
    [Test]
    public async Task AGradesJudge_SurvivesTheExport()
    {
        string At(object e) => "@" + JsonSerializer.Serialize(e);
        var lines = new[]
        {
            At(new { @event = "protocol", version = 1 }),
            At(new { @event = "mode", mode = SampleMode.Beta, repeat = 1, toolsets = new[] { SampleToolset.Alpha }, role = "puls/voice-af", llm = (string?)null, conversations = 1 }),
            At(new { @event = "question", mode = SampleMode.Beta, repeat = 1, task = "bobotie", turn = 1, session = "sess_1", text = "Maak bobotie" }),
            At(new { @event = "turn", mode = SampleMode.Beta, repeat = 1, task = "bobotie", turn = 1, session = "sess_1", question = "Maak bobotie", answer = "", role = (string?)null, seconds = 3.0 }),
            At(new { @event = "conversation", mode = SampleMode.Beta, repeat = 1, task = "bobotie", role = (string?)null, scores = new { }, errors = 0, seconds = 3.0,
                grades = new[] { new { grader = "meal-for-people", version = 1, score = 1.0, outcome = "Pass", reason = "feeds four", judge_model = "qwen3:8b", judge_prompt_hash = "abc123def456" } } }),
            At(new { @event = "mode_done", mode = SampleMode.Beta, repeat = 1 }),
        };
        var store = await Recorded(lines);

        string text = await Export(store);

        var conversation = (ConversationEnded)Parse(text.Split((char)10, StringSplitOptions.RemoveEmptyEntries).Single(l => l.Contains("\"event\":\"conversation\"")));
        Assert.That((conversation.Grades!.Single().JudgeModel, conversation.Grades!.Single().JudgePromptHash), Is.EqualTo(("qwen3:8b", "abc123def456")));
    }

    [Test]
    public async Task ATurnsInfrastructureFailure_SurvivesTheExport()
    {
        string At(object e) => "@" + JsonSerializer.Serialize(e);
        var lines = new[]
        {
            At(new { @event = "protocol", version = 1 }),
            At(new { @event = "mode", mode = SampleMode.Beta, repeat = 1, toolsets = new[] { SampleToolset.Alpha }, role = "puls/voice-af", llm = (string?)null, conversations = 1 }),
            At(new { @event = "question", mode = SampleMode.Beta, repeat = 1, task = "bobotie", turn = 1, session = "sess_1", text = "Maak bobotie" }),
            At(new { @event = "turn", mode = SampleMode.Beta, repeat = 1, task = "bobotie", turn = 1, session = "sess_1", question = "Maak bobotie", answer = "", role = (string?)null,
                seconds = 600.0, failure = "replay_miss" }),
            At(new { @event = "conversation", mode = SampleMode.Beta, repeat = 1, task = "bobotie", role = (string?)null, scores = new { }, errors = 0, seconds = 600.0 }),
            At(new { @event = "mode_done", mode = SampleMode.Beta, repeat = 1 }),
        };
        var store = await Recorded(lines);

        string text = await Export(store);

        var exported = text.Split((char)10, StringSplitOptions.RemoveEmptyEntries);
        var turn = (TurnEnded)Parse(exported.Single(l => l.Contains("\"event\":\"turn\"")));
        Assert.That(turn.Failure, Is.EqualTo(BenchmarkTurnFailure.ReplayMiss));
        Assert.That(Events(exported), Is.EqualTo(Events(lines)));
    }

    [Test]
    public async Task TheExportIsOneAtLinePerEvent_InTheOrderTheReaderTakesThem()
    {
        var store = await Recorded(StubLines());

        var lines = (await Export(store)).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.That(lines, Has.All.StartsWith("@{"));
        Assert.That(lines.Select(l => JsonDocument.Parse(l[1..]).RootElement.GetProperty("event").GetString()).First(), Is.EqualTo("protocol"));
        Assert.That(lines.Select(l => JsonDocument.Parse(l[1..]).RootElement.GetProperty("event").GetString()).Last(), Is.EqualTo("mode_done"));
    }

    [Test]
    public async Task AnUnknownRunIs404_AndNoKeyIs401()
    {
        var store = await Recorded(StubLines());
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var unknown = await Get(client, "/v1/admin/benchmarks/runs/nope/protocol", AdminKey);
        using var noKey = await Get(client, $"/v1/admin/benchmarks/runs/{Id}/protocol", null);
        using var wrongKey = await Get(client, $"/v1/admin/benchmarks/runs/{Id}/protocol", "not-the-key");

        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(noKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(wrongKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task TheRoute_ServesTheExportAsPlainUtf8Text()
    {
        var store = await Recorded(StubLines());
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var response = await Get(client, $"/v1/admin/benchmarks/runs/{Id}/protocol", AdminKey);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.ToString(), Is.EqualTo("text/plain; charset=utf-8"));
        string body = await response.Content.ReadAsStringAsync();
        Assert.That(Events(body.Split('\n', StringSplitOptions.RemoveEmptyEntries)), Is.EqualTo(Events(StubLines())));
    }

    private static Task<HttpResponseMessage> Get(HttpClient client, string path, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (key is not null) request.Headers.Add("x-api-key", key);
        return client.SendAsync(request);
    }
}
