using System.Net;
using System.Text.Json;
using LouisAgent.Api.Evals.Export;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals;

/// <summary>
/// The runner's summary lines (one per slice of a mode: overall, then each category) are kept on the run as slices,
/// written back by the export just as the runner wrote them, and served with the run with each slice's cost from the
/// ledger. The API works out no rate, and a run with no summary has none, never zeros.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public class SummaryStoredTests
{
    /// <summary>The id of the run every test records.</summary>
    private const string Id = "run123456789";

    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>When the run's lines start; each line is a second later than the one before.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The summary of one slice, with six of eight trials passed. A slice with too few trials has no rates, as the
    /// runner writes it.
    /// </summary>
    private static SummaryEvent Summary(string slice, bool tooFew, string[] tasks, int second) =>
        new(T0.AddSeconds(second), SampleMode.Beta, slice, 8, 6, 1, 1, 0, tasks, tooFew, tooFew ? null : 0.75,
            tooFew ? null : 1.0, tooFew ? null : 0.5,
            [new GraderCount("routed-to", GraderKind.FixedRule, 7, 1, 0, 0.875)],
            [new KindCount(GraderKind.FixedRule, 7, 1, 0)]);

    /// <summary>
    /// One run as the runner writes it: a mode with one conversation, its end, then two summaries (overall, and a
    /// category with too few trials).
    /// </summary>
    private static readonly string[] Run =
    [
        .. new EvalEvent[]
        {
            new ProtocolEvent(T0, ProtocolEvent.CurrentVersion, SampleEval.Beta, 7, "abc", RunSplit.Dev, null, null, [],
                []),
            new ModeEvent(T0.AddSeconds(1), SampleMode.Beta, 1, [SampleToolset.Alpha], null, null, 1),
            new ConversationEvent(T0.AddSeconds(2), SampleMode.Beta, 1, "a", SampleRole.Alpha, 0, 1.0, [],
                TrialOutcome.Pass, new Metrics(0, 0, [], 0, null, null, 1.0, null)),
            new ModeDoneEvent(T0.AddSeconds(3), SampleMode.Beta, 1, new RatesInfo(1.0, 1.0, 1, 0, 0)),
            Summary("overall", false, ["a", "b"], 4), Summary("language:af", true, ["a"], 5),
        }.Select(EvalProtocolJson.Line)
    ];

    /// <summary>Feeds the lines through the recorder into a fresh store, as a finished run would leave it.</summary>
    private static async Task<InMemoryEvalStore> Recorded(IEnumerable<string> lines, string? rescoreOf = null)
    {
        var store = new InMemoryEvalStore();
        var run = new RunningEval(Id, SampleEval.Beta, 1, rescoreOf: rescoreOf);

        // Start recording before the lines arrive, then end the run so the recorder finishes.
        var recording = new EvalRecorder(store).RecordAsync(run);
        foreach (string line in lines) run.Add(line);
        run.Complete(0);
        await recording;

        return store;
    }

    /// <summary>A slice keeps every field of its summary as the runner wrote it, and when it came.</summary>
    [Test]
    public void ASlice_KeepsItsSummarysFields_AsTheRunnerWroteThem()
    {
        var summary = Summary("overall", false, ["a", "b"], 4);

        var slice = EvalSlice.Of(summary);

        Assert.That(slice.ToEvent(T0), Is.EqualTo(summary).Using<SummaryEvent>((a, b) =>
            EvalProtocolJson.Line(a) == EvalProtocolJson.Line(b)));
    }

    /// <summary>The run keeps every slice, in the order the runner sent them.</summary>
    [Test]
    public async Task TheRecorder_KeepsEachSlice_OnTheRun_InTheOrderTheyCame()
    {
        var store = await Recorded(Run);

        var slices = (await store.GetRunAsync(Id))!.Slices!;
        Assert.That(slices.Select(s => (s.Mode, s.Slice, s.TooFew)),
            Is.EqualTo(new[] { (SampleMode.Beta, "overall", false), (SampleMode.Beta, "language:af", true) }));
    }

    /// <summary>A slice with too few trials has no rates at all, not zeros.</summary>
    [Test]
    public async Task ASliceWithTooFewTrials_HasNoRates_NullNeverZero()
    {
        var store = await Recorded(Run);

        var few = (await store.GetRunAsync(Id))!.Slices![1];
        Assert.That((few.TooFew, few.PassRate, few.PassAtK, few.PassHatK),
            Is.EqualTo((true, (double?)null, (double?)null, (double?)null)));
    }

    /// <summary>A run that sent no summary keeps no slices, not empty ones.</summary>
    [Test]
    public async Task ARunWithNoSummaryLine_HasNoSlices_NeverZeros()
    {
        var store = await Recorded(Run.Take(4));

        Assert.That((await store.GetRunAsync(Id))!.Slices, Is.Null);
    }

    /// <summary>A re-score keeps its slices like any other run.</summary>
    [Test]
    public async Task AReScore_KeepsItsSlicesToo()
    {
        var store = await Recorded(Run, rescoreOf: "old123456789");

        Assert.That((await store.GetRunAsync(Id))!.Slices, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// The export writes the summaries back as the runner wrote them, after the mode's last end, and adds no cost.
    /// </summary>
    [Test]
    public async Task TheExport_WritesTheSlicesBack_AsTheRunnerWroteThem_WithNoCost()
    {
        var store = await Recorded(Run);

        string text =
            await EvalProtocolExport.BuildAsync(store, (await store.GetRunAsync(Id))!, CancellationToken.None);

        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.TakeLast(3), Is.EqualTo(Run.TakeLast(3)), "the mode's end, then its two summaries");
        Assert.That(lines.Where(l => l.Contains("\"event\":\"summary\"")).All(l => !l.Contains("cost")), Is.True,
            "the ledger prices a slice, never the export");
    }

    /// <summary>A run with no slices exports no summary line.</summary>
    [Test]
    public async Task ARunWithNoSlices_ExportsNoSummaryLine()
    {
        var store = await Recorded(Run.Take(4));

        string text =
            await EvalProtocolExport.BuildAsync(store, (await store.GetRunAsync(Id))!, CancellationToken.None);

        Assert.That(text, Does.Not.Contain("\"summary\""));
    }

    /// <summary>
    /// The run's JSON serves each slice with its cost per trial and per pass from the ledger. A slice with too few
    /// trials has no rate, and a run from before the summaries has none at all.
    /// </summary>
    [Test]
    public async Task TheRunsJson_ServesEachSlice_WithItsCostFromTheLedger_AndNullForARunFromBefore()
    {
        // The recorded run with its total and two priced requests, and an older run with no summaries.
        var store = await Recorded(Run);
        var run = (await store.GetRunAsync(Id))!;
        await store.SaveRunAsync(run with { Total = new Total(0.8m, "USD", 1000, 2) });
        await store.AddRequestsAsync(Id, [Request("a", 0.5m), Request("b", 0.3m)]);
        await store.SaveRunAsync(new Run("old", SampleEval.Beta, 1, EvalRunStatus.Done, DateTimeOffset.UnixEpoch));

        // The API over that store.
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var json = await Get(client, $"/v1/admin/benchmarks/runs/{Id}");
        using var older = await Get(client, "/v1/admin/benchmarks/runs/old");

        var summaries = json.RootElement.GetProperty("summaries").EnumerateArray().ToList();
        var overall = summaries.Single(s => s.GetProperty("slice").GetString() == "overall");
        Assert.That(overall.GetProperty("mode").GetString(), Is.EqualTo(SampleMode.Beta));
        Assert.That(overall.GetProperty("cost_per_trial").GetDecimal(), Is.EqualTo(0.8m / 8));
        Assert.That(overall.GetProperty("cost_per_pass").GetDecimal(), Is.EqualTo(0.8m / 6));
        Assert.That(overall.GetProperty("graders")[0].GetProperty("score_mean").GetDouble(), Is.EqualTo(0.875));
        var few = summaries.Single(s => s.GetProperty("slice").GetString() == "language:af");
        Assert.That(few.GetProperty("too_few").GetBoolean(), Is.True);
        Assert.That(few.TryGetProperty("pass_rate", out var rate) && rate.ValueKind != JsonValueKind.Null, Is.False,
            "no rate: absent or null, never 0");
        Assert.That(older.RootElement.GetProperty("summaries").ValueKind, Is.EqualTo(JsonValueKind.Null),
            "a run from before 5c has none, never zeros");
    }

    /// <summary>One priced model request for a task of the run, as the ledger keeps it.</summary>
    private static Request Request(string task, decimal cost) =>
        new(Id, new TurnKey(SampleMode.Beta, 1, task, 1), DateTimeOffset.UnixEpoch, "turn", "sess_" + task, null,
            "claude-haiku-5-5", 100, 10, 0, 0, cost, 100);

    /// <summary>Asks the API for a path as the admin, checks it answered, and hands back its JSON.</summary>
    private static async Task<JsonDocument> Get(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-api-key", AdminKey);
        using var response = await client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
}
