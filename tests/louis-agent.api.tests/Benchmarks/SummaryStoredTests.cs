using LouisAgent.Testing;
using System.Net;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Tests.Endpoints.Routing;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks;

// F18 step 5c.2: the runner's `summary` lines (one per slice of a mode: overall, then each category) are read into a typed record, kept on the run (both stores), written back by
// the export as the runner wrote them (its fields only, no cost), and served with the run with each slice's cost from the ledger. The API computes no rate. A run with no summary line
// (every run before 5c) reads as none, never as zeros (ADR 0007). Written first: the protocol read no summary line, and the run kept none.
public class SummaryStoredTests
{
    private const string Id = "run123456789";
    private const string AdminKey = "admin-secret";

    private static string At(object e) => "@" + JsonSerializer.Serialize(e);

    private static string SummaryLine(string mode = SampleMode.Beta, string slice = "overall", bool tooFew = false, double? passRate = 0.75, string[]? tasks = null)
    {
        var fields = new Dictionary<string, object?>
        {
            ["event"] = "summary", ["t"] = 1700000009.0, ["mode"] = mode, ["slice"] = slice, ["n"] = 8, ["passed"] = 6, ["failed"] = 1, ["unknown"] = 1, ["infrastructure"] = 0,
            ["tasks"] = tasks ?? ["a", "b"], ["too_few"] = tooFew,
            ["graders"] = new[] { new { grader = "routed-to", kind = "FixedRule", passed = 7, failed = 1, unknown = 0 } },
            ["kinds"] = new[] { new { kind = "FixedRule", passed = 7, failed = 1, unknown = 0 } },
            ["scores"] = new[] { new { key = "priced_ratio", mean = 0.5, n = 8 } },
        };
        if (passRate is { } rate)
        {
            fields["pass_rate"] = rate;
            fields["pass_at_k"] = 1.0;
            fields["pass_hat_k"] = 0.5;
        }
        return At(fields);
    }

    private static readonly string[] Run =
    [
        At(new { @event = "protocol", t = 1700000000.5, version = 1, eval = "puls-cook", eval_version = 7, commit = "abc", split = "dev", graders = Array.Empty<object>(), models = Array.Empty<object>() }),
        At(new { @event = "mode", t = 1700000001.0, mode = SampleMode.Beta, repeat = 1, toolsets = new[] { SampleToolset.Alpha }, role = (string?)null, llm = (string?)null, conversations = 0 }),
        At(new { @event = "conversation", t = 1700000002.0, mode = SampleMode.Beta, repeat = 1, task = "a", role = "puls-cook", scores = new { }, errors = 0, seconds = 1.0, outcome = "Pass" }),
        At(new { @event = "mode_done", t = 1700000003.0, mode = SampleMode.Beta, repeat = 1, pass_at_k = 1.0, pass_hat_k = 1.0, k = 1, infrastructure = 0, retried = 0 }),
        SummaryLine(),
        SummaryLine(slice: "language:af", tooFew: true, passRate: null, tasks: ["a"]),
    ];

    private static async Task<InMemoryBenchmarkStore> Recorded(IEnumerable<string> lines, string? rescoreOf = null)
    {
        var store = new InMemoryBenchmarkStore();
        var run = new BenchmarkRun(Id, "puls-cook", 1, rescoreOf: rescoreOf);
        var recording = new BenchmarkRecorder(store).RecordAsync(run);
        foreach (string line in lines) run.Add(line);
        run.Complete(0);
        await recording;
        return store;
    }

    // --- reading the line

    [Test]
    public void ASummaryLine_IsReadIntoATypedSlice_WithItsFieldsAsTheRunnerWroteThem()
    {
        var slice = ((SummaryReported)Parse(SummaryLine())).Slice;

        Assert.That((slice.Mode, slice.Slice, slice.N, slice.Passed, slice.Failed, slice.Unknown, slice.Infrastructure),
            Is.EqualTo((SampleMode.Beta, "overall", 8, 6, 1, 1, 0)));
        Assert.That(slice.Tasks, Is.EqualTo(new[] { "a", "b" }));
        Assert.That((slice.TooFew, slice.PassRate, slice.PassAtK, slice.PassHatK), Is.EqualTo((false, (double?)0.75, (double?)1.0, (double?)0.5)));
        Assert.That(slice.Graders, Is.EqualTo(new[] { new SliceGrader("routed-to", BenchmarkGraderKind.FixedRule, 7, 1, 0) }));
        Assert.That(slice.Kinds, Is.EqualTo(new[] { new SliceKind(BenchmarkGraderKind.FixedRule, 7, 1, 0) }));
        Assert.That(slice.Scores, Is.EqualTo(new[] { new SliceScore("priced_ratio", 0.5, 8) }));
    }

    [Test]
    public void ASliceWithTooFewTrials_HasNoRates_NullNeverZero()
    {
        var slice = ((SummaryReported)Parse(SummaryLine(tooFew: true, passRate: null))).Slice;

        Assert.That((slice.TooFew, slice.PassRate, slice.PassAtK, slice.PassHatK), Is.EqualTo((true, (double?)null, (double?)null, (double?)null)));
    }

    [Test]
    public void ASummaryLineThatCannotBeRead_IsALogLine_NeverAnException() =>
        Assert.That(Parse(SampleMode.Fill("""@{"event":"summary","mode":"<ModeBeta>"}""")), Is.InstanceOf<LogLine>());

    // --- kept on the run

    [Test]
    public async Task TheRecorder_KeepsEachSlice_OnTheRun_InTheOrderTheyCame()
    {
        var store = await Recorded(Run);

        var slices = (await store.GetRunAsync(Id))!.Slices!;
        Assert.That(slices.Select(s => (s.Mode, s.Slice, s.TooFew)), Is.EqualTo(new[] { (SampleMode.Beta, "overall", false), (SampleMode.Beta, "language:af", true) }));
    }

    [Test]
    public async Task ARunWithNoSummaryLine_HasNoSlices_NeverZeros()
    {
        var store = await Recorded(Run.Take(4));

        Assert.That((await store.GetRunAsync(Id))!.Slices, Is.Null);
    }

    [Test]
    public async Task AReScore_KeepsItsSlicesToo()
    {
        var store = await Recorded(Run, rescoreOf: "old123456789");

        Assert.That((await store.GetRunAsync(Id))!.Slices, Has.Count.EqualTo(2));
    }

    // --- written back

    [Test]
    public async Task TheExport_WritesTheSlicesBack_AsTheRunnerWroteThem_WithNoCost()
    {
        var store = await Recorded(Run);
        var run = (await store.GetRunAsync(Id))!;

        string text = await BenchmarkProtocolExport.BuildAsync(store, run, CancellationToken.None);

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var exported = lines.Where(l => l.Contains("\"event\":\"summary\"")).Select(l => ((SummaryReported)Parse(l)).Slice).ToList();
        // A record holds its lists by reference, so the slices are compared by what they say.
        Assert.That(JsonSerializer.Serialize(exported), Is.EqualTo(JsonSerializer.Serialize(run.Slices)));
        Assert.That(lines.Where(l => l.Contains("\"event\":\"summary\"")).All(l => !l.Contains("cost")), Is.True, "the ledger prices a slice, never the export");
        string[] events = lines.Select(l => JsonDocument.Parse(l[1..]).RootElement.GetProperty("event").GetString()!).ToArray();
        Assert.That(events.TakeLast(3), Is.EqualTo(new[] { "mode_done", "summary", "summary" }), "after the mode's last mode_done");
    }

    [Test]
    public async Task ARunWithNoSlices_ExportsNoSummaryLine()
    {
        var store = await Recorded(Run.Take(4));

        string text = await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync(Id))!, CancellationToken.None);

        Assert.That(text, Does.Not.Contain("\"summary\""));
    }

    // --- served with the run, each slice priced from the ledger

    [Test]
    public async Task TheRunsJson_ServesEachSlice_WithItsCostFromTheLedger_AndNullForARunFromBefore()
    {
        var store = await Recorded(Run);
        var run = (await store.GetRunAsync(Id))!;
        await store.SaveRunAsync(run with { Total = new Total(0.8m, "USD", 1000, 2) });
        await store.AddRequestsAsync(Id, [Request("a", 0.5m), Request("b", 0.3m)]);
        await store.SaveRunAsync(new Run("old", "puls-cook", 1, BenchmarkRunStatus.Done, DateTimeOffset.UnixEpoch));
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
        var few = summaries.Single(s => s.GetProperty("slice").GetString() == "language:af");
        Assert.That(few.GetProperty("too_few").GetBoolean(), Is.True);
        Assert.That(few.TryGetProperty("pass_rate", out var rate) && rate.ValueKind != JsonValueKind.Null, Is.False, "no rate: absent or null, never 0");
        Assert.That(older.RootElement.GetProperty("summaries").ValueKind, Is.EqualTo(JsonValueKind.Null), "a run from before 5c has none, never zeros");
    }

    private static Request Request(string task, decimal cost) =>
        new(Id, new TurnKey(SampleMode.Beta, 1, task, 1), DateTimeOffset.UnixEpoch, "turn", "sess_" + task, null, "claude-haiku-5-5", 100, 10, 0, 0, cost, 100);

    private static async Task<JsonDocument> Get(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-api-key", AdminKey);
        using var response = await client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
}
