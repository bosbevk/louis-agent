using System.Net;
using System.Net.Http.Json;
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
        Assert.That(evals!.Evals.Single().LastRun, Is.EqualTo(RunRow.Of(run)));
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
