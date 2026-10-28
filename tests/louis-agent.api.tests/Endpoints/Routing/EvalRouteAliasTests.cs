using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The admin's eval routes live under <c>/admin/evals/…</c> now, and the old <c>/admin/benchmarks/…</c> ones still
/// answer the same, so a saved link or an older page keeps working until a TODO line removes them.
/// </summary>
/// <remarks>
/// Expand, then contract: both names answer while the clients move over, and only then does the old one go.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
public class EvalRouteAliasTests
{
    /// <summary>The admin key the test API is started with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>The id of the one stored run.</summary>
    private const string RunId = "8f7802b9a72e";

    /// <summary>When the stored run started.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Starts the API with the admin key and a store holding one finished run.</summary>
    private static async Task<WebApplication> StartAsync()
    {
        var store = new InMemoryEvalStore();
        await store.SaveRunAsync(new Run(RunId, SampleEval.Beta, 1, EvalRunStatus.Done, T0, T0.AddMinutes(5), 0));

        var app = ApiTestHost.Build(adminKey: AdminKey, store: store);
        await app.StartAsync();
        return app;
    }

    /// <summary>Sends a request with the admin key and reads back its status and body.</summary>
    private static async Task<(HttpStatusCode Status, string Body)> SendAsync(WebApplication app, HttpMethod method,
        string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("x-api-key", AdminKey);
        if (body is not null) request.Content = JsonContent.Create(body);

        using var response = await app.GetTestClient().SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Each read under its new name answers exactly what it answers under its old one. A route that isn't there
    /// answers a bare 404 with no body, so even an eval the test host doesn't know tells the two apart.
    /// </summary>
    [TestCase("/v1/admin/evals/runs/" + RunId, "/v1/admin/benchmarks/runs/" + RunId)]
    [TestCase("/v1/admin/evals/runs/" + RunId + "/protocol", "/v1/admin/benchmarks/runs/" + RunId + "/protocol")]
    [TestCase("/v1/admin/evals/runs/" + RunId + "/reviews", "/v1/admin/benchmarks/runs/" + RunId + "/reviews")]
    [TestCase("/v1/admin/evals/runs/current", "/v1/admin/benchmarks/runs/current")]
    [TestCase("/v1/admin/evals/" + SampleEval.Beta + "/trend",
        "/v1/admin/benchmarks/checks/" + SampleEval.Beta + "/trend")]
    [TestCase("/v1/admin/evals/" + SampleEval.Beta + "/manual", "/v1/admin/benchmarks/" + SampleEval.Beta + "/manual")]
    public async Task ARead_AnswersTheSame_UnderItsNewAndItsOldName(string newPath, string oldPath)
    {
        await using var app = await StartAsync();

        var fresh = await SendAsync(app, HttpMethod.Get, newPath);
        var old = await SendAsync(app, HttpMethod.Get, oldPath);

        Assert.That(old.Body, Is.Not.Empty, "the old route answers with a body of its own");
        Assert.That(fresh, Is.EqualTo(old));
    }

    /// <summary>A note can be written under the new name, and the run read under the old one carries it.</summary>
    [Test]
    public async Task ANote_WrittenUnderTheNewName_IsReadUnderTheOldOne()
    {
        await using var app = await StartAsync();

        var written = await SendAsync(app, HttpMethod.Post, $"/v1/admin/evals/runs/{RunId}/notes",
            new { text = "the cook got slower" });
        var run = await SendAsync(app, HttpMethod.Get, $"/v1/admin/benchmarks/runs/{RunId}");

        Assert.That(written.Status, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(run.Body, Does.Contain("the cook got slower"));
    }
}
