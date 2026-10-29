using System.Net;
using System.Text.Json;
using LouisAgent.Api.Evals.Export;
using LouisAgent.Api.Evals.Reports.Trends;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals;

/// <summary>
/// The pass rates the runner writes when a mode is done follow the run all the way: kept per mode, written back by
/// the export, carried by each trend point and served with the run. The API works none of them out itself, and a mode
/// whose end carries no rates has none, never 0.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public class PassRatesStoredTests
{
    /// <summary>The id of the run every test records.</summary>
    private const string Id = "run123456789";

    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>When the run's lines start, and when the older run in the trend test started.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// One eval run as the runner writes it: one mode, repeated twice, each repeat's end with these rates.
    /// </summary>
    private static string[] EvalRun(RatesInfo? first, RatesInfo? second) =>
    [
        .. new EvalEvent[]
        {
            new ProtocolEvent(T0, ProtocolEvent.CurrentVersion, SampleEval.Beta, 3, "abc", RunSplit.Dev, null, null, [],
                []),
            new ModeEvent(T0.AddSeconds(1), SampleMode.Beta, 1, [SampleToolset.Alpha], null, null, 0),
            new ModeDoneEvent(T0.AddSeconds(2), SampleMode.Beta, 1, first),
            new ModeEvent(T0.AddSeconds(3), SampleMode.Beta, 2, [SampleToolset.Alpha], null, null, 0),
            new ModeDoneEvent(T0.AddSeconds(4), SampleMode.Beta, 2, second),
        }.Select(EvalProtocolJson.Line)
    ];

    /// <summary>The run with its rates: the second repeat left one trial out and ran one again.</summary>
    private static readonly string[] Rated =
        EvalRun(new RatesInfo(1.0, 1.0, 1, 0, 0), new RatesInfo(1.0, 0.5, 2, 1, 1));

    /// <summary>Feeds the lines through the recorder into a fresh store, as a finished run would leave it.</summary>
    private static async Task<InMemoryEvalStore> Recorded(IEnumerable<string> lines, string id = Id)
    {
        var store = new InMemoryEvalStore();
        var run = new RunningEval(id, SampleEval.Beta, 2);

        // Start recording before the lines arrive, then end the run so the recorder finishes.
        var recording = new EvalRecorder(store).RecordAsync(run);
        foreach (string line in lines) run.Add(line);
        run.Complete(0);
        await recording;

        return store;
    }

    /// <summary>A conversation that passed, of the mode's first repeat.</summary>
    private static Conversation Passed() =>
        new(new ConversationKey(SampleMode.Beta, 1, SampleScenario.Alpha), 1, null, null, 0, 1,
            Outcome: TrialOutcome.Pass);

    /// <summary>Each repeat of a mode keeps its own rates in the store.</summary>
    [Test]
    public async Task TheRecorder_KeepsEachRepeatsRatesOnItsMode()
    {
        var store = await Recorded(Rated);

        var modes = (await store.GetRunAsync(Id))!.Modes!;

        Assert.That(modes.Select(m => (m.Repeat, m.Rates)),
            Is.EqualTo(new (int, ModeRates?)[]
            {
                (1, new ModeRates(1.0, 1.0, 1, 0, 0)), (2, new ModeRates(1.0, 0.5, 2, 1, 1)),
            }));
    }

    /// <summary>When no trial counted, the rates are missing but the counts still come through.</summary>
    [Test]
    public async Task ARateLeftOut_BecauseNoTrialCounted_IsNull_WithItsCounts()
    {
        var store = await Recorded(EvalRun(new RatesInfo(null, null, 1, 3, 2), null));

        var modes = (await store.GetRunAsync(Id))!.Modes!;

        Assert.That(modes[0].Rates, Is.EqualTo(new ModeRates(null, null, 1, 3, 2)));
    }

    /// <summary>A mode whose end carries no rates keeps none.</summary>
    [Test]
    public async Task AModesEndWithoutRates_LeavesItsModeWithout()
    {
        var store = await Recorded(EvalRun(null, null));

        Assert.That((await store.GetRunAsync(Id))!.Modes!.Select(m => m.Rates), Is.All.Null);
    }

    /// <summary>Exporting a run gives back each mode's end with the same rates.</summary>
    [Test]
    public async Task AnExportedRun_HasTheSameRates()
    {
        var store = await Recorded(Rated);

        string text =
            await EvalProtocolExport.BuildAsync(store, (await store.GetRunAsync(Id))!, CancellationToken.None);

        var done = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(EvalProtocolJson.Read)
            .OfType<ModeDoneEvent>().Select(e => e.Rates).ToList();
        Assert.That(done, Is.EqualTo(new RatesInfo?[] { new(1.0, 1.0, 1, 0, 0), new(1.0, 0.5, 2, 1, 1) }));
    }

    /// <summary>A trend point shows the rates of the last repeat of its mode in that run.</summary>
    [Test]
    public async Task ATrendPoint_CarriesTheRatesOfItsRunsLastRepeatOfTheMode()
    {
        var store = await Recorded(Rated);
        await store.SaveConversationAsync(Id, Passed());

        var point = (await EvalTrends.ComputeAsync(store, SampleEval.Beta)).Modes.Single().Points.Single();

        Assert.That((point.PassAtK, point.PassHatK, point.K), Is.EqualTo(((double?)1.0, (double?)0.5, (int?)2)));
    }

    /// <summary>A trend point for a run whose modes have no rates has none.</summary>
    [Test]
    public async Task ATrendPointOfARunWithoutRates_HasNullRates()
    {
        var store = new InMemoryEvalStore();
        await store.SaveRunAsync(new Run("old", SampleEval.Beta, 1, EvalRunStatus.Done, T0,
            Modes: [new Mode(SampleMode.Beta, 1, [SampleToolset.Alpha], null, null)]));
        await store.SaveConversationAsync("old", Passed());

        var point = (await EvalTrends.ComputeAsync(store, SampleEval.Beta)).Modes.Single().Points.Single();

        Assert.That((point.PassAtK, point.PassHatK, point.K), Is.EqualTo(((double?)null, (double?)null, (int?)null)));
    }

    /// <summary>The run's JSON gives each mode its rates, and null for a mode that has none.</summary>
    [Test]
    public async Task TheRunsJson_NamesEachModesRates_AndNullWhereThereAreNone()
    {
        // The recorded run, plus a mode with no rates on the end.
        var store = await Recorded(Rated);
        await store.SaveRunAsync((await store.GetRunAsync(Id))! with
        {
            Modes =
            [
                .. (await store.GetRunAsync(Id))!.Modes!,
                new Mode(SampleMode.Alpha, 1, [SampleToolset.Alpha], null, null)
            ]
        });

        // The API over that store, asked for the run.
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
