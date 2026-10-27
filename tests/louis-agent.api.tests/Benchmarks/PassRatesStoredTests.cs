using LouisAgent.Testing;
using System.Net;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Tests.Endpoints.Routing;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks;

// F18 step 5a.2: the rates the runner wrote on a mode's mode_done line are read, kept per mode, written back by the export, carried by each trend point and
// served with the run. The API computes none of it (the formula is the harness's); a line from before them reads as null, never as 0 (ADR 0007).
// Written first: ModeEnded, Mode, the export, the trend point and the run's JSON had no rates.
public class PassRatesStoredTests
{
    private const string Id = "run123456789";
    private const string AdminKey = "admin-secret";
    private static readonly DateTimeOffset T0 = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    private static string At(object e) => "@" + JsonSerializer.Serialize(e);

    private static readonly string[] EvalRun =
    [
        At(new
        {
            @event = "protocol", t = 1700000000.5, version = 1, eval = SampleEval.Beta, eval_version = 3,
            commit = "abc", split = "dev", graders = Array.Empty<object>(), models = Array.Empty<object>()
        }),
        At(new
        {
            @event = "mode", t = 1700000001.0, mode = SampleMode.Beta, repeat = 1,
            toolsets = new[] { SampleToolset.Alpha }, role = (string?)null, llm = (string?)null, conversations = 0
        }),
        At(new
        {
            @event = "mode_done", t = 1700000002.0, mode = SampleMode.Beta, repeat = 1, pass_at_k = 1.0,
            pass_hat_k = 1.0, k = 1, infrastructure = 0, retried = 0
        }),
        At(new
        {
            @event = "mode", t = 1700000003.0, mode = SampleMode.Beta, repeat = 2,
            toolsets = new[] { SampleToolset.Alpha }, role = (string?)null, llm = (string?)null, conversations = 0
        }),
        At(new
        {
            @event = "mode_done", t = 1700000004.0, mode = SampleMode.Beta, repeat = 2, pass_at_k = 1.0,
            pass_hat_k = 0.5, k = 2, infrastructure = 1, retried = 1
        }),
    ];

    private static async Task<InMemoryBenchmarkStore> Recorded(IEnumerable<string> lines, string id = Id)
    {
        var store = new InMemoryBenchmarkStore();
        var run = new BenchmarkRun(id, SampleEval.Beta, 2);
        var recording = new BenchmarkRecorder(store).RecordAsync(run);
        foreach (string line in lines) run.Add(line);
        run.Complete(0);
        await recording;
        return store;
    }

    // --- reading the line

    [Test]
    public void AModeDone_CarriesItsRates()
    {
        var ended = (ModeEnded)Parse(EvalRun[4]);

        Assert.That(ended.Rates, Is.EqualTo(new RatesInfo(1.0, 0.5, 2, 1, 1)));
    }

    [Test]
    public void AModeDoneFromBeforeTheRates_ReadsAsNull_NeverZero()
    {
        var ended = (ModeEnded)Parse(SampleMode.Fill("""@{"event":"mode_done","mode":"<ModeBeta>","repeat":1}"""));

        Assert.That(ended.Rates, Is.Null);
    }

    [Test]
    public void ARateLeftOut_BecauseNoTrialCounted_ReadsAsNull_WithItsCounts()
    {
        var ended = (ModeEnded)Parse(SampleMode.Fill(
            """@{"event":"mode_done","mode":"<ModeBeta>","repeat":1,"k":1,"infrastructure":3,"retried":2}"""));

        Assert.That(ended.Rates, Is.EqualTo(new RatesInfo(null, null, 1, 3, 2)));
    }

    // --- keeping them

    [Test]
    public async Task TheRecorder_KeepsEachRepeatsRatesOnItsMode()
    {
        var store = await Recorded(EvalRun);

        var modes = (await store.GetRunAsync(Id))!.Modes!;

        Assert.That(modes.Select(m => (m.Repeat, m.Rates)), Is.EqualTo(new (int, ModeRates?)[]
        {
            (1, new ModeRates(1.0, 1.0, 1, 0, 0)),
            (2, new ModeRates(1.0, 0.5, 2, 1, 1)),
        }));
    }

    [Test]
    public async Task ARunFromBeforeTheRates_HasNoRatesOnItsModes()
    {
        var store = await Recorded(EvalRun.Select((line, i) =>
            i is 2 or 4 ? line[..line.IndexOf(",\"pass_at_k\"", StringComparison.Ordinal)] + "}" : line));

        Assert.That((await store.GetRunAsync(Id))!.Modes!.Select(m => m.Rates), Is.All.Null);
    }

    // --- writing them back

    [Test]
    public async Task AnExportedRun_ReadsBackWithTheSameRates()
    {
        var store = await Recorded(EvalRun);

        string text =
            await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync(Id))!, CancellationToken.None);

        var done = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(Parse).OfType<ModeEnded>()
            .Select(e => e.Rates).ToList();
        Assert.That(done, Is.EqualTo(new RatesInfo?[] { new(1.0, 1.0, 1, 0, 0), new(1.0, 0.5, 2, 1, 1) }));
    }

    // --- the trend

    [Test]
    public async Task ATrendPoint_CarriesTheRatesOfItsRunsLastRepeatOfTheMode()
    {
        var store = await Recorded(EvalRun);
        await store.SaveConversationAsync(Id,
            new Conversation(new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, null,
                JsonDocument.Parse("{}").RootElement.Clone(), 0, 1));

        var point = (await BenchmarkTrends.ComputeAsync(store, SampleEval.Beta)).Modes.Single().Points.Single();

        Assert.That((point.PassAtK, point.PassHatK, point.K), Is.EqualTo(((double?)1.0, (double?)0.5, (int?)2)));
    }

    [Test]
    public async Task ATrendPointOfARunFromBefore_HasNullRates()
    {
        var store = new InMemoryBenchmarkStore();
        await store.SaveRunAsync(new Run("old", SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0,
            Modes: [new Mode(SampleMode.Beta, 1, [SampleToolset.Alpha], null, null)]));
        await store.SaveConversationAsync("old",
            new Conversation(new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, null,
                JsonDocument.Parse("{}").RootElement.Clone(), 0, 1));

        var point = (await BenchmarkTrends.ComputeAsync(store, SampleEval.Beta)).Modes.Single().Points.Single();

        Assert.That((point.PassAtK, point.PassHatK, point.K), Is.EqualTo(((double?)null, (double?)null, (int?)null)));
    }

    // --- served with the run

    [Test]
    public async Task TheRunsJson_NamesEachModesRates_AndNullWhereThereAreNone()
    {
        var store = await Recorded(EvalRun);
        await store.SaveRunAsync((await store.GetRunAsync(Id))! with
        {
            Modes =
            [
                .. (await store.GetRunAsync(Id))!.Modes!,
                new Mode(SampleMode.Alpha, 1, [SampleToolset.Alpha], null, null)
            ]
        });
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/admin/benchmarks/runs/{Id}");
        request.Headers.Add("x-api-key", AdminKey);
        using var response = await client.SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var modes = json.RootElement.GetProperty("modes").EnumerateArray().ToList();
        Assert.That(modes[1].GetProperty("pass_at_k").GetDouble(), Is.EqualTo(1.0));
        Assert.That(modes[1].GetProperty("pass_hat_k").GetDouble(), Is.EqualTo(0.5));
        Assert.That(modes[1].GetProperty("k").GetInt32(), Is.EqualTo(2));
        Assert.That(modes[1].GetProperty("infrastructure").GetInt32(), Is.EqualTo(1));
        Assert.That(modes[1].GetProperty("retried").GetInt32(), Is.EqualTo(1));
        Assert.That(modes[2].GetProperty("pass_at_k").ValueKind, Is.EqualTo(JsonValueKind.Null),
            "no rates: null, never 0");
        Assert.That(modes[2].GetProperty("k").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }
}
