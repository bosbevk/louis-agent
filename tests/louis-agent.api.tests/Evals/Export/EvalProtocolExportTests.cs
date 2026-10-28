using System.Net;
using System.Text.Json;
using LouisAgent.Api.Evals.Export;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Tests.Evals.Export;

/// <summary>
/// A stored run can be written back out as the runner's own lines. Record a run, export it and read it back, and you
/// get the same run, key for key and in the same order, apart from the times. The lines come from the stub run the
/// runner is checked against, so a recorded run and an exported one answer to the same reader.
/// </summary>
public class EvalProtocolExportTests
{
    /// <summary>The id of the run every test records.</summary>
    private const string Id = "run123456789";

    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>
    /// The event lines of the stub run as protocol version 1, frozen until step 7c.3, when the API reads the runner's
    /// own golden (now version 2).
    /// </summary>
    private static string[] StubLines() =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "stub-run.v1.txt"))
            .Where(l => l.StartsWith('@')).ToArray();

    /// <summary>Writes an event as the runner does: an @ and then its JSON.</summary>
    private static string At(object e) => "@" + JsonSerializer.Serialize(e);

    /// <summary>Feeds the lines through the recorder into a fresh store, as a finished run would leave it.</summary>
    private static async Task<InMemoryEvalStore> Recorded(IEnumerable<string> lines)
    {
        var store = new InMemoryEvalStore();
        var run = new RunningEval(Id, "cook", 1);

        // Start recording before the lines arrive, then end the run so the recorder finishes.
        var recording = new EvalRecorder(store).RecordAsync(run);
        foreach (string line in lines) run.Add(line);
        run.Complete(0);
        await recording;

        return store;
    }

    /// <summary>
    /// Turns an event into text we can compare: its type and its fields, without the time, since the export has no
    /// clock of the run's own.
    /// </summary>
    private static string Plain(Event e)
    {
        var node = JsonSerializer.SerializeToNode(e, e.GetType())!.AsObject();
        node.Remove("At");
        return e.GetType().Name + node.ToJsonString();
    }

    /// <summary>Reads the lines back into events, skips the plain log lines, and makes each one comparable.</summary>
    private static IReadOnlyList<string> Events(IEnumerable<string> lines) =>
        lines.Where(l => l.StartsWith('@')).Select(Parse).Where(e => e is not LogLine).Select(Plain).ToList();

    /// <summary>Exports the recorded run as text.</summary>
    private static async Task<string> Export(InMemoryEvalStore store) =>
        await EvalProtocolExport.BuildAsync(store, (await store.GetRunAsync(Id))!, CancellationToken.None);

    /// <summary>The stub run, recorded, exported and read back, is the same run.</summary>
    [Test]
    public async Task APythonRunExportedAndReadBack_IsTheSameRun_KeyForKey()
    {
        var store = await Recorded(StubLines());

        string text = await Export(store);

        Assert.That(Events(text.Split('\n', StringSplitOptions.RemoveEmptyEntries)), Is.EqualTo(Events(StubLines())));
    }

    /// <summary>
    /// The question a human review asks stays on the first line of the export, so a re-score of the run still says
    /// what was asked.
    /// </summary>
    [Test]
    public async Task AHumanReviewsQuestion_SurvivesTheExport()
    {
        var lines = new[]
        {
            At(new
            {
                @event = "protocol",
                version = 1,
                eval = SampleEval.Beta,
                eval_version = 9,
                commit = "abc",
                graders = new object[]
                {
                    new
                    {
                        name = "meal-for-people.human",
                        version = 1,
                        kind = "Human",
                        question = "Is this a meal?"
                    }
                },
                models = Array.Empty<object>()
            }),
            At(new
            {
                @event = "mode",
                mode = SampleMode.Beta,
                repeat = 1,
                toolsets = new[] { SampleToolset.Alpha },
                role = (string?)null,
                llm = (string?)null,
                conversations = 0
            }),
            At(new
            {
                @event = "mode_done",
                mode = SampleMode.Beta,
                repeat = 1
            }),
        };
        var store = await Recorded(lines);

        string text = await Export(store);

        var begin = (ProtocolStarted)Parse(text.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]);
        Assert.That(begin.Graders!.Single().Question, Is.EqualTo("Is this a meal?"));
    }

    /// <summary>A held-out run keeps its split on the first line, so the export reads back as held out.</summary>
    [Test]
    public async Task AHeldOutRun_KeepsItsSplitOnTheBeginLine_SoAnExportedRunReadsBackAsHeldOut()
    {
        var lines = new[]
        {
            At(new
            {
                @event = "protocol",
                t = 1700000000.5,
                version = 1,
                eval = SampleEval.Beta,
                eval_version = 3,
                commit = "abc123",
                split = "held_out",
                graders = Array.Empty<object>(),
                models = Array.Empty<object>()
            }),
            At(new
            {
                @event = "mode",
                t = 1700000001.0,
                mode = SampleMode.Beta,
                repeat = 1,
                toolsets = new[] { SampleToolset.Alpha },
                role = (string?)null,
                llm = (string?)null,
                conversations = 0
            }),
            At(new
            {
                @event = "mode_done",
                t = 1700000005.0,
                mode = SampleMode.Beta,
                repeat = 1
            }),
        };
        var store = await Recorded(lines);

        string text = await Export(store);

        var begin = Parse(text.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]);
        Assert.That(text.Split('\n')[0], Does.Contain("\"split\":\"held_out\""));
        Assert.That(((ProtocolStarted)begin).Split, Is.EqualTo(EvalRunSplit.HeldOut));
    }

    /// <summary>
    /// An eval run keeps its eval, graders and models, and each conversation its grades and outcome, written in plain
    /// ASCII as the harness writes it.
    /// </summary>
    [Test]
    public async Task AnEvalRunKeepsItsEvalGradersAndModels_AndEachConversationsGradesAndOutcome()
    {
        // A whole eval run: the begin line, a mode, one question with its streamed answer, the turn, the graded
        // conversation and the mode's end.
        var lines = new[]
        {
            At(new
            {
                @event = "protocol",
                t = 1700000000.5,
                version = 1,
                eval = SampleEval.Beta,
                eval_version = 2,
                commit = "abc123",
                graders = new[]
                {
                    new
                    {
                        name = "cook-no-error",
                        version = 1,
                        kind = "FixedRule"
                    }
                },
                models = new[]
                {
                    new
                    {
                        mode = SampleMode.Beta,
                        level = "Agent",
                        role = "puls/voice-af",
                        model = "claude-haiku-5-5"
                    }
                },
            }),
            At(new
            {
                @event = "mode",
                t = 1700000001.0,
                mode = SampleMode.Beta,
                repeat = 1,
                toolsets = new[] { SampleToolset.Gamma, SampleToolset.Alpha },
                role = "puls/voice-af",
                llm = (string?)null,
                conversations = 1
            }),
            At(new
            {
                @event = "question",
                t = 1700000001.0,
                mode = SampleMode.Beta,
                repeat = 1,
                task = "bobotie",
                turn = 1,
                session = "sess_1",
                text = "Maak bobotie"
            }),
            At(new
            {
                @event = "sse",
                t = 1700000002.25,
                mode = SampleMode.Beta,
                repeat = 1,
                task = "bobotie",
                turn = 1,
                type = "content_block_delta",
                data = new { delta = new { text = "Hé" } }
            }),
            At(new
            {
                @event = "turn",
                t = 1700000003.0,
                mode = SampleMode.Beta,
                repeat = 1,
                task = "bobotie",
                turn = 1,
                session = "sess_1",
                question = "Maak bobotie",
                answer = "Hé",
                role = SampleRole.Alpha,
                continued = (string?)null,
                puls_calls = 1,
                specialist_calls = 6,
                hosts = new[] { "www.checkers.co.za" },
                seconds = 30.5,
                error = (string?)null,
            }),
            At(new
            {
                @event = "conversation",
                t = 1700000004.0,
                mode = SampleMode.Beta,
                repeat = 1,
                task = "bobotie",
                role = SampleRole.Alpha,
                scores = new { routed_right = true },
                errors = 0,
                seconds = 30.5,
                grades = new[]
                {
                    new
                    {
                        grader = "cook-no-error",
                        version = 1,
                        score = 1.0,
                        outcome = "Pass",
                        reason = "no error"
                    }
                },
                outcome = "Pass",
            }),
            At(new
            {
                @event = "mode_done",
                t = 1700000005.0,
                mode = SampleMode.Beta,
                repeat = 1
            }),
        };
        var store = await Recorded(lines);

        string text = await Export(store);

        Assert.That(Events(text.Split('\n', StringSplitOptions.RemoveEmptyEntries)), Is.EqualTo(Events(lines)));
        Assert.That(text.All(c => c < 128), Is.True, "ASCII only, as the harness writes it: the é is escaped");
    }

    /// <summary>The model that judged a grade, and its prompt's hash, come through the export with the grade.</summary>
    [Test]
    public async Task AGradesJudge_SurvivesTheExport()
    {
        var lines = Conversation(seconds: 3.0, failure: null, grades: new[]
        {
            new
            {
                grader = "meal-for-people",
                version = 1,
                score = 1.0,
                outcome = "Pass",
                reason = "feeds four",
                judge_model = "qwen3:8b",
                judge_prompt_hash = "abc123def456"
            }
        });
        var store = await Recorded(lines);

        string text = await Export(store);

        var conversation = (ConversationEnded)Parse(text.Split((char)10, StringSplitOptions.RemoveEmptyEntries)
            .Single(l => l.Contains("\"event\":\"conversation\"")));
        Assert.That((conversation.Grades!.Single().JudgeModel, conversation.Grades!.Single().JudgePromptHash),
            Is.EqualTo(("qwen3:8b", "abc123def456")));
    }

    /// <summary>A turn that failed for an infrastructure reason keeps that reason through the export.</summary>
    [Test]
    public async Task ATurnsInfrastructureFailure_SurvivesTheExport()
    {
        var lines = Conversation(seconds: 600.0, failure: "replay_miss", grades: null);
        var store = await Recorded(lines);

        string text = await Export(store);

        var exported = text.Split((char)10, StringSplitOptions.RemoveEmptyEntries);
        var turn = (TurnEnded)Parse(exported.Single(l => l.Contains("\"event\":\"turn\"")));
        Assert.That(turn.Failure, Is.EqualTo(EvalTurnFailure.ReplayMiss));
        Assert.That(Events(exported), Is.EqualTo(Events(lines)));
    }

    /// <summary>The export is one @ line per event, from the begin line to a mode's end.</summary>
    [Test]
    public async Task TheExportIsOneAtLinePerEvent_InTheOrderTheReaderTakesThem()
    {
        var store = await Recorded(StubLines());

        var lines = (await Export(store)).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.That(lines, Has.All.StartsWith("@{"));
        Assert.That(lines.Select(l => JsonDocument.Parse(l[1..]).RootElement.GetProperty("event").GetString()).First(),
            Is.EqualTo("protocol"));
        Assert.That(lines.Select(l => JsonDocument.Parse(l[1..]).RootElement.GetProperty("event").GetString()).Last(),
            Is.EqualTo("mode_done"));
    }

    /// <summary>The export route doesn't find a run it doesn't know, and refuses a missing or wrong key.</summary>
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

    /// <summary>The route serves the export as plain UTF-8 text, and it reads back as the same run.</summary>
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

    /// <summary>
    /// A short run of one conversation that asks for bobotie and gets no answer. The turn can carry a failure and the
    /// conversation its grades, for the two tests that check one of them.
    /// </summary>
    private static string[] Conversation(double seconds, string? failure, object? grades)
    {
        // The turn and the conversation leave a field out when the test has nothing for it, as the runner does.
        var turn = new Dictionary<string, object?>
        {
            ["event"] = "turn",
            ["mode"] = SampleMode.Beta,
            ["repeat"] = 1,
            ["task"] = "bobotie",
            ["turn"] = 1,
            ["session"] = "sess_1",
            ["question"] = "Maak bobotie",
            ["answer"] = "",
            ["role"] = null,
            ["seconds"] = seconds,
        };
        if (failure is not null) turn["failure"] = failure;
        var conversation = new Dictionary<string, object?>
        {
            ["event"] = "conversation",
            ["mode"] = SampleMode.Beta,
            ["repeat"] = 1,
            ["task"] = "bobotie",
            ["role"] = null,
            ["scores"] = new { },
            ["errors"] = 0,
            ["seconds"] = seconds,
        };
        if (grades is not null) conversation["grades"] = grades;

        return
        [
            At(new
            {
                @event = "protocol",
                version = 1
            }),
            At(new
            {
                @event = "mode",
                mode = SampleMode.Beta,
                repeat = 1,
                toolsets = new[] { SampleToolset.Alpha },
                role = "puls/voice-af",
                llm = (string?)null,
                conversations = 1
            }),
            At(new
            {
                @event = "question",
                mode = SampleMode.Beta,
                repeat = 1,
                task = "bobotie",
                turn = 1,
                session = "sess_1",
                text = "Maak bobotie"
            }),
            At(turn),
            At(conversation),
            At(new
            {
                @event = "mode_done",
                mode = SampleMode.Beta,
                repeat = 1
            }),
        ];
    }

    /// <summary>Asks the API for a path, with the key if there is one.</summary>
    private static Task<HttpResponseMessage> Get(HttpClient client, string path, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (key is not null) request.Headers.Add("x-api-key", key);
        return client.SendAsync(request);
    }
}
