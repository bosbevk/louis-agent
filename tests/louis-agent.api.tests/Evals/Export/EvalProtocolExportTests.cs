using System.Net;
using LouisAgent.Api.Evals.Export;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Kinds;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Evals.Export;

/// <summary>
/// A stored run can be written back out as the runner's own lines. Record a run, export it, and you get the very same
/// lines back, character for character: every event, every field, its time and its order. The lines are the runner's
/// golden of the stub's run and the contract's golden, so a recorded run and an exported one answer to the same
/// reader, and a re-score reads an export as it would the run.
/// </summary>
public class EvalProtocolExportTests
{
    /// <summary>The id of the run every test records.</summary>
    private const string Id = "run123456789";

    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>The lines of a fixture linked into this project.</summary>
    private static string[] Fixture(string name) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

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

    /// <summary>Exports the recorded run, line by line.</summary>
    private static async Task<string[]> Export(InMemoryEvalStore store) =>
        (await EvalProtocolExport.BuildAsync(store, (await store.GetRunAsync(Id))!, CancellationToken.None))
        .Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// The contract's golden, recorded and exported, is the same lines: the dataset, the human review's question, the
    /// judge on a grade, what the conversation measured, the rates and the summary's mean scores, each at its time.
    /// </summary>
    [Test]
    public async Task TheContractsGolden_ExportsBackLineForLine()
    {
        string[] golden = Fixture("eval-protocol.jsonl");

        Assert.That(await Export(await Recorded(golden)), Is.EqualTo(golden));
    }

    /// <summary>The stub's run, recorded and exported, is the same lines, its stream events too.</summary>
    [Test]
    public async Task TheStubsRun_ExportsBackLineForLine()
    {
        string[] run = Fixture("stub-run.txt");

        Assert.That(await Export(await Recorded(run)), Is.EqualTo(run));
    }

    /// <summary>A turn that failed for an infrastructure reason keeps that reason through the export.</summary>
    [Test]
    public async Task ATurnsInfrastructureFailure_SurvivesTheExport()
    {
        // The golden, its turn failed by an API refusal.
        string[] lines =
        [
            .. Fixture("eval-protocol.jsonl").Select(l => l.StartsWith("{\"event\":\"turn\"")
                ? l.Replace("\"failure\":null", "\"failure\":\"api-error\"")
                : l)
        ];

        string[] exported = await Export(await Recorded(lines));

        var turn = exported.Select(EvalProtocolJson.Read).OfType<TurnEvent>().Single();
        Assert.That(turn.Failure, Is.EqualTo(TurnFailure.ApiError));
        Assert.That(exported, Is.EqualTo(lines));
    }

    /// <summary>The export route doesn't find a run it doesn't know, and refuses a missing or wrong key.</summary>
    [Test]
    public async Task AnUnknownRunIs404_AndNoKeyIs401()
    {
        var store = await Recorded(Fixture("stub-run.txt"));
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

    /// <summary>The route serves the export as plain UTF-8 text, the same lines the run came as.</summary>
    [Test]
    public async Task TheRoute_ServesTheExportAsPlainUtf8Text()
    {
        string[] run = Fixture("stub-run.txt");
        var store = await Recorded(run);
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var response = await Get(client, $"/v1/admin/benchmarks/runs/{Id}/protocol", AdminKey);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.ToString(), Is.EqualTo("text/plain; charset=utf-8"));
        string body = await response.Content.ReadAsStringAsync();
        Assert.That(body.Split('\n', StringSplitOptions.RemoveEmptyEntries), Is.EqualTo(run));
    }

    /// <summary>Asks the API for a path, with the key if there is one.</summary>
    private static Task<HttpResponseMessage> Get(HttpClient client, string path, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (key is not null) request.Headers.Add("x-api-key", key);
        return client.SendAsync(request);
    }
}
