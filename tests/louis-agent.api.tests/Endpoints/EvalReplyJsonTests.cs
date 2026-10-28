using System.Net;
using System.Text.Json;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the exact JSON <c>GET /v1/admin/evals</c> sends, so the portal keeps reading it however the code that builds it
/// is shaped. The wire is snake_case, and a property's place counts: the reply is compared as text.
/// </summary>
public class EvalReplyJsonTests
{
    /// <summary>The admin key the test host is built with, and that each request sends.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>An eval that never ran, written out whole: what it is, no runs, and "never run".</summary>
    [Test]
    public async Task AnEvalThatNeverRan_IsWrittenWhole()
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: new InMemoryEvalStore(),
            catalog: Catalog());
        await app.StartAsync();

        string json = await GetAsync(app, "/v1/admin/evals");

        Assert.That(json, Is.EqualTo(Compact("""
                                             {
                                               "evals": [
                                                 {
                                                   "name": "eval-beta", "identity": "identity-alpha",
                                                   "version": 3, "proves": "A meal", "manual": false,
                                                   "dataset": { "name": "meals", "version": 2 },
                                                   "categories": ["town"],
                                                   "graders": [
                                                     { "name": "priced", "version": 1, "kind": "fixed-rule" }
                                                   ],
                                                   "modes": [], "models": [], "never_run": true, "runs": []
                                                 }
                                               ],
                                               "store": { "configured": true, "error": null }
                                             }
                                             """)));
    }

    /// <summary>A recorded run is the stored run's own row, beside its modes and their rates.</summary>
    [Test]
    public async Task ARecordedRun_IsTheStoredRunsRow_WithItsModes()
    {
        // One finished run of the eval, on one mode.
        var store = new InMemoryEvalStore();
        var at = new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
        var mode = new Mode(SampleMode.Beta, 1, [SampleToolset.Beta], SampleRole.Beta, null,
            new ModeRates(0.5, 0.25, 2, 0, 0));
        await store.SaveRunAsync(new Run("r1", SampleEval.Beta, 1, EvalRunStatus.Done, at, Modes: [mode]));

        await using var app = ApiTestHost.Build(adminKey: AdminKey, store: store, catalog: Catalog());
        await app.StartAsync();

        using var json = JsonDocument.Parse(await GetAsync(app, "/v1/admin/evals"));

        var run = json.RootElement.GetProperty("evals")[0].GetProperty("runs")[0];
        Assert.That(Names(run), Is.EqualTo("run,modes"));
        Assert.That(Names(run.GetProperty("run")), Is.EqualTo(
            "id,check,repeat,status,exit_code,started_at,ended_at,total,store_error,models,eval,eval_version,commit,"
            + "rescore_of,graders"));
        Assert.That(Names(run.GetProperty("modes")[0]), Is.EqualTo(
            "name,repeat,toolsets,role,llm,pass_at_k,pass_hat_k,k,infrastructure,retried"));
    }

    /// <summary>A catalogue of one eval with one grader, a dataset and a category, as the runner lists it.</summary>
    private static EvalList Catalog() => new(EvalList.Merge(
    [
        new EvalLine(SampleEval.Beta, 3, "A meal", SampleIdentity.Alpha, null, new EvalDatasetLine("meals", 2),
            ["town"], [new EvalGraderLine("priced", 1, GraderKind.FixedRule)])
    ]));

    /// <summary>Asks the admin route and hands back the body; the call must have been a 200.</summary>
    private static async Task<string> GetAsync(WebApplication app, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-api-key", AdminKey);

        using var response = await app.GetTestClient().SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>The JSON written on one line with no spaces, its properties kept in their order.</summary>
    private static string Compact(string json) => JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement);

    /// <summary>The object's property names in the order they were written, joined by commas.</summary>
    private static string Names(JsonElement element) => string.Join(",", element.EnumerateObject().Select(p => p.Name));
}
