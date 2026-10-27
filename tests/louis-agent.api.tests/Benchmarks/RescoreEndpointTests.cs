using System.Net;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Tests.Benchmarks.Runs;
using LouisAgent.Api.Tests.Endpoints.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Benchmarks;

// F18 step 5b.2 (ADR 0039, POST /{resource}/{verb}): POST /v1/admin/benchmarks/runs/{id}/rescore starts a re-score of a stored run: 201 with the new run's
// Location, 404 for a run it doesn't hold, 409 while another run runs (as a start answers), 400 for a run of a check the runner does not list (only an eval's transcripts can be graded
// again). The new run is recorded with rescore_of, and costs 0 requests and $0: known, because nothing was asked. Written first: the route did not exist.
public class RescoreEndpointTests
{
    private const string AdminKey = "admin-secret";

    private string _workspace = "";

    [SetUp]
    public void SetUp()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-rescore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_workspace, recursive: true);

    private sealed record Host(WebApplication App, HttpClient Client, InMemoryBenchmarkStore Store, BenchmarkRunner Runner, BenchmarkRecorder Recorder) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>The API over the real runner: the eval check "stub", an in-memory store with a recorder.</summary>
    private async Task<Host> StartAsync()
    {
        var store = new InMemoryBenchmarkStore();
        var recorder = new BenchmarkRecorder(store);
        var catalog = new BenchmarkCatalog([new BenchmarkCheck("stub", "Stub")]);
        var runner = new BenchmarkRunner(catalog, _workspace, "http://127.0.0.1:1/", RunnerBuild.Dll);
        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, runner: runner, recorder: recorder, catalog: catalog);
        await app.StartAsync();
        return new Host(app, app.GetTestClient(), store, runner, recorder);
    }

    /// <summary>Runs the stub through the real runner and stores it: the run a re-score reads.</summary>
    private static async Task<string> StoredStubRunAsync(Host host)
    {
        var run = host.Runner.Start("stub", 1)!;
        var recording = host.Recorder.RecordAsync(run);
        await foreach (string _ in host.Runner.EventsAsync(run.Id)) { }
        await recording;
        Assume.That(run.ExitCode, Is.EqualTo(0), "dotnet isn't here to run the eval runner");
        return run.Id;
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/admin/benchmarks/runs/{id}/rescore");
        request.Headers.Add("x-api-key", AdminKey);
        return client.SendAsync(request);
    }

    [Test]
    public async Task ARescore_Is201WithTheNewRunsLocation_AndIsRecordedAsARescoreCostingNothing()
    {
        await using var host = await StartAsync();
        string stored = await StoredStubRunAsync(host);

        using var response = await Post(host.Client, stored);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string id = body.RootElement.GetProperty("id").GetString()!;
        Assert.That(id, Is.Not.EqualTo(stored));
        Assert.That(response.Headers.Location!.OriginalString, Is.EqualTo($"/v1/admin/benchmarks/runs/{id}"));

        await foreach (string _ in host.Runner.EventsAsync(id)) { }
        await host.Recorder.RecordingOf(id)!;
        var rescored = (await host.Store.GetRunAsync(id))!;
        Assert.That(rescored.RescoreOf, Is.EqualTo(stored));
        Assert.That((rescored.Total!.Cost, rescored.Total.Requests), Is.EqualTo(((decimal?)0m, 0)), "known: nothing was asked, never null (unknown) and never the original's");
        Assert.That((await host.Store.GetRunAsync(stored))!.RescoreOf, Is.Null, "the old run is not changed");
    }

    [Test]
    public async Task AnUnknownRun_Is404()
    {
        await using var host = await StartAsync();

        using var response = await Post(host.Client, "nope");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task WhileAnotherRunRuns_Is409_AsAStartIs()
    {
        await using var host = await StartAsync();
        string stored = await StoredStubRunAsync(host);
        // A run of the stub with every repeat: the runner takes longer to start and finish than this request takes to arrive.
        var busy = host.Runner.Start("stub", BenchmarkRunner.MaxRepeat)!;

        using var response = await Post(host.Client, stored);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await foreach (string _ in host.Runner.EventsAsync(busy.Id)) { }
    }

    [Test]
    public async Task ARunOfACheckTheRunnerDoesNotList_Is400_BecauseThereIsNoEvalToGradeItAgain()
    {
        await using var host = await StartAsync();
        await host.Store.SaveRunAsync(new BenchmarkDocuments.Run("gone-run", "gone-check", 1, BenchmarkRunStatus.Done, DateTimeOffset.UnixEpoch));

        using var response = await Post(host.Client, "gone-run");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task WithoutTheAdminKey_Is401()
    {
        await using var host = await StartAsync();

        using var response = await host.Client.PostAsync("/v1/admin/benchmarks/runs/x/rescore", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
