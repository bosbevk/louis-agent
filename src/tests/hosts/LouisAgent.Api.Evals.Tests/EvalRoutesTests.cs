using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Evals.Tests.Fakes;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Evals.Runs.Store;
using Microsoft.Extensions.AI;
using static LouisAgent.Api.Evals.Tests.EvalsTestHost;

namespace LouisAgent.Api.Evals.Tests;

/// <summary>
/// The four eval routes, on the API in memory: behind the admin key, a list of the host's evals, a run started and
/// followed at its own address until it has ended, an eval's runs, and every refusal of a start with its reason.
/// </summary>
public class EvalRoutesTests
{
    /// <summary>Each route answers 401 without the admin's key.</summary>
    [TestCase("GET", "/v1/admin/evals")]
    [TestCase("POST", "/v1/admin/evals/greets/runs")]
    [TestCase("GET", "/v1/admin/evals/runs/20261123-043012-greets")]
    [TestCase("DELETE", "/v1/admin/evals/runs/20261123-043012-greets")]
    [TestCase("GET", "/v1/admin/evals/greets/runs")]
    public async Task EachRoute_AnswersUnauthorized_WithoutTheAdminKey(string method, string path)
    {
        await using var host = await StartAsync(Answering("Hello."));
        host.Client.DefaultRequestHeaders.Remove("x-api-key");

        using var response = await host.Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>The list names the eval, its agent and its count of conversations, with no run yet.</summary>
    [Test]
    public async Task TheList_NamesTheEval_ItsAgentAndItsConversations()
    {
        await using var host = await StartAsync(Answering("Hello."));

        var reply = await host.Client.GetFromJsonAsync<EvalsReply>("/v1/admin/evals", RunJson.Options);

        Assert.That(reply!.Evals, Is.EqualTo(new[] { new EvalRow(Eval, Agent, 1, null) }));
    }

    /// <summary>
    /// A live start answers 202 with the run's address; read until it ends, it is a finished run whose conversation
    /// passed, kept in its file under the data folder and listed after.
    /// </summary>
    [Test]
    public async Task ALiveStart_IsAccepted_AndItsAddressGivesAFinishedRun_KeptAndListed()
    {
        await using var host = await StartAsync(Answering("Hello."));

        using var started = await host.StartRunAsync();
        Assert.That(started.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        var run = await host.EndedAsync(started.Headers.Location!);

        Assert.That((run.Status, run.Kind, run.Passed), Is.EqualTo((RunStatus.Finished, RunKind.Live, 1)));
        Assert.That(File.Exists(Path.Combine(host.RunsFolder, run.Id + ".json")), Is.True);
        var runs = await host.Client.GetFromJsonAsync<IReadOnlyList<RunRow>>($"/v1/admin/evals/{Eval}/runs",
            RunJson.Options);
        Assert.That(runs!.Select(row => row.Id), Is.EqualTo(new[] { run.Id }));
        var evals = await host.Client.GetFromJsonAsync<EvalsReply>("/v1/admin/evals", RunJson.Options);
        // Compared as their JSON: a row holds a list, which a record's own equality compares by reference.
        Assert.That(JsonSerializer.Serialize(evals!.Evals.Single().LastRun, RunJson.Options),
            Is.EqualTo(JsonSerializer.Serialize(RunRow.Of(run), RunJson.Options)));
    }

    /// <summary>
    /// F126.3.4: the list names each eval's last live run that finished apart from its last run, so after a replay the
    /// live run it played back is still named; an eval never run live names none.
    /// </summary>
    [Test]
    public async Task TheList_NamesTheLastLiveRun_ApartFromTheLastRun()
    {
        await using var host = await StartAsync(Answering("Hello."));
        var before = await host.Client.GetFromJsonAsync<EvalsReply>("/v1/admin/evals", RunJson.Options);

        var live = await host.RunToTheEndAsync();
        var replay = await host.RunToTheEndAsync(RunKind.Replay);
        var after = await host.Client.GetFromJsonAsync<EvalsReply>("/v1/admin/evals", RunJson.Options);

        Assert.That(before!.Evals.Single().LastLiveRun, Is.Null);
        Assert.That(after!.Evals.Single().LastRun!.Id, Is.EqualTo(replay.Id));
        Assert.That(after.Evals.Single().LastLiveRun!.Id, Is.EqualTo(live.Id));
    }

    /// <summary>A second start while one runs is a 409; a model that waits for the test holds the first open.</summary>
    [Test]
    public async Task ASecondStart_WhileOneRuns_IsAConflict()
    {
        using var held = new ManualResetEventSlim();
        var waiting = new FakeChatClient(_ =>
        {
            held.Wait(TimeSpan.FromSeconds(30));
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello."));
        });
        await using var host = await StartAsync(waiting);

        using var first = await host.StartRunAsync();
        using var second = await host.StartRunAsync();
        held.Set();

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await host.EndedAsync(first.Headers.Location!);
    }

    /// <summary>
    /// F126.3.4: DELETE on the going run's address stops it: 204, and the run is then kept as stopped, with its end
    /// time and a reason that says a person stopped it.
    /// </summary>
    [Test]
    public async Task StoppingTheGoingRun_KeepsItAsStopped()
    {
        using var held = new ManualResetEventSlim();
        await using var host = await StartAsync(Waiting(held));
        using var started = await host.StartRunAsync();

        using var stop = await host.Client.DeleteAsync(started.Headers.Location!);
        var run = await host.EndedAsync(started.Headers.Location!);
        held.Set();

        Assert.That(stop.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((run.Status, run.EndedAt is not null), Is.EqualTo((RunStatus.Stopped, true)));
        Assert.That(run.StoppedBecause, Does.Contain("stopped"));
    }

    /// <summary>A stop of an unknown run is a 404, and of a run that has ended a 409 with its reason.</summary>
    [Test]
    public async Task StoppingAnUnknownRun_IsNotFound_AndAnEndedOne_AConflict()
    {
        await using var host = await StartAsync(Answering("Hello."));
        var ended = await host.RunToTheEndAsync();

        using var unknown = await host.Client.DeleteAsync("/v1/admin/evals/runs/20261123-043012-nobody");
        using var over = await host.Client.DeleteAsync($"/v1/admin/evals/runs/{ended.Id}");

        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(over.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await over.Content.ReadAsStringAsync(), Does.Contain(ended.Id));
    }

    /// <summary>
    /// A run cut by the host's shutdown is left in its file as it was, still running, so it reads as cut off: only a
    /// person's stop is kept as stopped. Nothing writes it after the host has gone (FX251: once, a write after the
    /// shutdown raced the folder's clean-up on Linux).
    /// </summary>
    [Test]
    public async Task ARunCutByTheHostsShutdown_IsLeftAsItWas()
    {
        // The run reaches a model that is still thinking when its host shuts down.
        var thinking = new ThinkingModel();
        await using var keeper = await StartAsync(Answering("Hello."));
        var shuttingDown = await StartAsync(thinking, sameFoldersAs: keeper);
        using var started = await shuttingDown.StartRunAsync();
        await thinking.Asked.WaitAsync(TimeSpan.FromSeconds(10));

        // The host goes, and a moment later the run's file is read: a late write would have landed by then.
        await shuttingDown.DisposeAsync();
        await Task.Delay(LateWriteWindow);

        string id = started.Headers.Location!.ToString()[EvalHandlers.RunAddress.Length..];
        string file = await File.ReadAllTextAsync(Path.Combine(keeper.RunsFolder, id + ".json"));
        Assert.That(file, Does.Contain("\"status\": \"running\"").Or.Contain("\"status\":\"running\""));
    }

    /// <summary>How long a test waits after a host has gone for a write that shouldn't come.</summary>
    private static readonly TimeSpan LateWriteWindow = TimeSpan.FromMilliseconds(500);

    /// <summary>A model that waits for the test before it answers, so a run stays going.</summary>
    private static FakeChatClient Waiting(ManualResetEventSlim held) => new(Enumerable.Repeat<Func<IList<ChatMessage>,
        ChatResponse>>(_ =>
    {
        held.Wait(TimeSpan.FromSeconds(30));
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello."));
    }, 5).ToArray());

    /// <summary>
    /// F126.4: the list gives an eval's row its run before, and an eval's runs carry each row's criteria missed or
    /// flipped, and the run a re-grade re-graded.
    /// </summary>
    [Test]
    public async Task TheListAndTheRuns_CarryTheRunBefore_TheCriteria_AndARegradesRun()
    {
        await using var host = await StartAsync(Answering("Hello."),
            Answering("""{"reason":"It doesn't.","met":false}"""), criterion: "Greets the person");
        var first = await host.RunToTheEndAsync();
        var second = await host.RunToTheEndAsync();
        using var regrade = await host.Client.PostAsync($"/v1/admin/evals/runs/{second.Id}/regrade", null);
        var regraded = await host.EndedAsync(regrade.Headers.Location!);

        var reply = await host.Client.GetFromJsonAsync<EvalsReply>("/v1/admin/evals", RunJson.Options);
        var rows = await host.Client.GetFromJsonAsync<IReadOnlyList<RunRow>>($"/v1/admin/evals/{Eval}/runs",
            RunJson.Options);

        var row = reply!.Evals.Single();
        Assert.That((row.LastRun!.Id, row.RunBefore!.Id), Is.EqualTo((regraded.Id, second.Id)));
        Assert.That(row.CountChange(), Is.Zero);
        Assert.That(rows![0].RegradeOf, Is.EqualTo(second.Id));
        Assert.That(rows.Single(r => r.Id == first.Id).Criteria,
            Is.EqualTo(new[] { new CriterionTally("Greets the person", 1, 0) }));
    }

    /// <summary>An eval the host doesn't have is a 404.</summary>
    [Test]
    public async Task AnUnknownEval_IsNotFound()
    {
        await using var host = await StartAsync(Answering("Hello."));
        var request = new StartRunRequest(RunKind.Live, 1);

        using var response = await host.Client.PostAsJsonAsync("/v1/admin/evals/nobody/runs", request,
            RunJson.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Repeats that are neither 1 nor 3 are a 400 that says so.</summary>
    [Test]
    public async Task RepeatsOfTwo_AreABadRequest()
    {
        await using var host = await StartAsync(Answering("Hello."));

        using var response = await host.StartRunAsync(repeats: 2);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("1 or 3"));
    }

    /// <summary>A host with no data folder has nowhere to keep a run, and refuses its start with that reason.</summary>
    [Test]
    public async Task AHostWithNoDataFolder_RefusesTheStart_SayingWhy()
    {
        await using var host = await StartAsync(Answering("Hello."), withDataFolder: false);

        using var response = await host.StartRunAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain(EvalsOptions.DataFolderSetting));
    }
}
