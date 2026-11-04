using System.Net;
using System.Text.Json;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.IntegrationTests.Evals;

/// <summary>
/// The re-score endpoint grades a stored run again: 201 with the new run's address, 404 for a run it doesn't hold, 409
/// while another run runs, and 400 for a run of an eval the runner doesn't list. The new run is recorded as a
/// re-score and costs nothing, known as nothing because no model was asked.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API's routes follow one rule, under /v1</seealso>
[Category("Integration")]
public class RescoreEndpointTests
{
    /// <summary>The admin key the test API is built with.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>A fresh folder per test for the runner to work in.</summary>
    private string _workspace = "";

    /// <summary>Makes the test's own working folder.</summary>
    [SetUp]
    public void SetUp()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "louis-rescore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
    }

    /// <summary>Deletes the test's working folder.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_workspace, recursive: true);

    /// <summary>The running API and the parts a test reaches past it: the store, the runner and the recorder.</summary>
    private sealed record Host(
        WebApplication App,
        HttpClient Client,
        InMemoryEvalStore Store,
        EvalProcess Runner,
        EvalRecorder Recorder) : IAsyncDisposable
    {
        /// <summary>Stops the API when the test is done with it.</summary>
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>Starts the API over the real runner, with the stub eval and an in-memory store that records.</summary>
    private async Task<Host> StartAsync()
    {
        // The store, its recorder, and a runner that knows only the stub.
        var store = new InMemoryEvalStore();
        var recorder = new EvalRecorder(store);
        var catalog = new EvalList([new EvalEntry("stub", "Stub")]);
        var runner = new EvalProcess(catalog, _workspace, "http://127.0.0.1:1/", RunnerBuild.Dll);

        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, runner: runner, recorder: recorder,
            catalog: catalog);
        await app.StartAsync();

        return new Host(app, app.GetTestClient(), store, runner, recorder);
    }

    /// <summary>Runs the stub for real and stores it, so a re-score has something to read.</summary>
    private static async Task<string> StoredStubRunAsync(Host host)
    {
        var run = host.Runner.Start("stub", 1)!;
        var recording = host.Recorder.RecordAsync(run);
        await foreach (string _ in host.Runner.EventsAsync(run.Id))
        {
        }

        await recording;
        Outside.Tool(run.ExitCode == 0, "dotnet isn't here to run the eval runner");
        return run.Id;
    }

    /// <summary>Asks the API, with the admin key, to re-score a run.</summary>
    private static Task<HttpResponseMessage> Post(HttpClient client, string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/admin/benchmarks/runs/{id}/rescore");
        request.Headers.Add("x-api-key", AdminKey);
        return client.SendAsync(request);
    }

    /// <summary>
    /// A re-score answers 201 with the new run's address, and the new run is stored as a re-score of the old one that
    /// cost nothing, while the old run stays as it was.
    /// </summary>
    [Test]
    public async Task ARescore_Is201WithTheNewRunsLocation_AndIsRecordedAsARescoreCostingNothing()
    {
        await using var host = await StartAsync();
        string stored = await StoredStubRunAsync(host);

        using var response = await Post(host.Client, stored);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string id = body.RootElement.GetProperty("id").GetString()!;
        Assert.That(id, Is.Not.EqualTo(stored));
        Assert.That(response.Headers.Location!.OriginalString, Is.EqualTo($"/v1/admin/benchmarks/runs/{id}"));

        // Let the re-score finish and be recorded before reading it back.
        await foreach (string _ in host.Runner.EventsAsync(id))
        {
        }

        await host.Recorder.RecordingOf(id)!;
        var rescored = (await host.Store.GetRunAsync(id))!;
        Assert.That(rescored.RescoreOf, Is.EqualTo(stored));
        Assert.That((rescored.Total!.Cost, rescored.Total.Requests), Is.EqualTo(((decimal?)0m, 0)),
            "known: nothing was asked, never null (unknown) and never the original's");
        Assert.That((await host.Store.GetRunAsync(stored))!.RescoreOf, Is.Null, "the old run is not changed");
    }

    /// <summary>A run the store doesn't hold is a 404.</summary>
    [Test]
    public async Task AnUnknownRun_Is404()
    {
        await using var host = await StartAsync();

        using var response = await Post(host.Client, "nope");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>A re-score while another run runs is a 409, as starting a run would be.</summary>
    [Test]
    public async Task WhileAnotherRunRuns_Is409_AsAStartIs()
    {
        await using var host = await StartAsync();
        string stored = await StoredStubRunAsync(host);
        // A run of the stub with every repeat: the runner takes longer to start and finish than this request takes to
        // arrive.
        var busy = host.Runner.Start("stub", EvalProcess.MaxRepeat)!;

        using var response = await Post(host.Client, stored);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        // Let the busy run finish before the test ends.
        await foreach (string _ in host.Runner.EventsAsync(busy.Id))
        {
        }
    }

    /// <summary>A run of an eval the runner no longer lists is a 400: there's nothing to grade it with.</summary>
    [Test]
    public async Task ARunOfACheckTheRunnerDoesNotList_Is400_BecauseThereIsNoEvalToGradeItAgain()
    {
        await using var host = await StartAsync();
        await host.Store.SaveRunAsync(new EvalDocuments.Run("gone-run", "gone-check", 1, EvalRunStatus.Done,
            DateTimeOffset.UnixEpoch));

        using var response = await Post(host.Client, "gone-run");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>Without the admin key, the endpoint answers 401.</summary>
    [Test]
    public async Task WithoutTheAdminKey_Is401()
    {
        await using var host = await StartAsync();

        using var response = await host.Client.PostAsync("/v1/admin/benchmarks/runs/x/rescore", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
