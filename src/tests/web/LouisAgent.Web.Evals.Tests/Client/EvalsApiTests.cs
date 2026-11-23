using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Storage;

namespace LouisAgent.Web.Evals.Tests.Client;

/// <summary>
/// The pages read the eval routes through one small client over the site's API client, with the run's own JSON
/// options: the list, a run whole, and an eval's runs; a run the host doesn't have is none.
/// </summary>
public class EvalsApiTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 6, 36, 33, TimeSpan.Zero);

    /// <summary>The list asks its route and gives the rows the API sent.</summary>
    [Test]
    public async Task ListAsync_AsksTheListRoute_AndGivesTheRows()
    {
        var row = new EvalRow("guide", "louis-agent-guide", 1, RunRow.Of(Run()));
        var (api, stub) = Api(_ => Json(new EvalsReply([row])));

        var reply = await api.ListAsync();

        Assert.That(stub.Requests.Single().Url, Is.EqualTo("http://localhost/v1/admin/evals"));
        // Compared as their JSON: a row holds a list, which a record's own equality compares by reference.
        Assert.That(JsonSerializer.Serialize(reply.Evals, RunJson.Options),
            Is.EqualTo(JsonSerializer.Serialize(new[] { row }, RunJson.Options)));
    }

    /// <summary>A run comes back whole, equal to what the API wrote, compared as its JSON.</summary>
    [Test]
    public async Task FindRunAsync_AsksTheRunsRoute_AndGivesTheRunWhole()
    {
        var run = Run();
        var (api, stub) = Api(_ => Json(run));

        var found = await api.FindRunAsync(run.Id);

        Assert.That(stub.Requests.Single().Url, Is.EqualTo($"http://localhost/v1/admin/evals/runs/{run.Id}"));
        Assert.That(JsonSerializer.Serialize(found, RunJson.Options),
            Is.EqualTo(JsonSerializer.Serialize(run, RunJson.Options)));
    }

    /// <summary>A run the host doesn't have is none, not an exception.</summary>
    [Test]
    public async Task ARunTheHostDoesntHave_IsNone()
    {
        var (api, _) = Api(_ => Status(HttpStatusCode.NotFound));

        Assert.That(await api.FindRunAsync("20261123-063633-guide"), Is.Null);
    }

    /// <summary>An eval's runs come back in the order the API sent them.</summary>
    [Test]
    public async Task ListRunsAsync_AsksTheEvalsRuns_AndKeepsTheirOrder()
    {
        var newer = RunRow.Of(Run() with { Id = "20261124-090000-guide" });
        var older = RunRow.Of(Run());
        var (api, stub) = Api(_ => Json(new[] { newer, older }));

        var rows = await api.ListRunsAsync("guide");

        Assert.That(stub.Requests.Single().Url, Is.EqualTo("http://localhost/v1/admin/evals/guide/runs"));
        Assert.That(rows.Select(row => row.Id), Is.EqualTo(new[] { newer.Id, older.Id }));
    }

    /// <summary>A refused key comes through as the API client's own exception, untouched.</summary>
    [Test]
    public void ARefusedKey_IsTheApiClientsOwnException()
    {
        var (api, _) = Api(_ => Status(HttpStatusCode.Unauthorized));

        var refused = Assert.ThrowsAsync<AgentApiException>(() => api.FindRunAsync("20261123-063633-guide"));

        Assert.That(refused!.Status, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// F126.3.4: a start posts the kind and the repeats to the eval's runs, written as the routes read them, and
    /// answers the run's id from the address the API gave.
    /// </summary>
    [Test]
    public async Task AStart_PostsTheKindAndRepeats_AndAnswersTheRunsId()
    {
        var (api, stub) = Api(_ => new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Headers = { Location = new Uri("/v1/admin/evals/runs/20261123-063633-guide", UriKind.Relative) }
        });

        var start = await api.StartAsync("guide", RunKind.Live, 3);

        var (method, url, body) = stub.Requests.Single();
        Assert.That((method, url), Is.EqualTo((HttpMethod.Post, "http://localhost/v1/admin/evals/guide/runs")));
        Assert.That(JsonSerializer.Deserialize<StartRunRequest>(body!, RunJson.Options),
            Is.EqualTo(new StartRunRequest(RunKind.Live, 3)));
        Assert.That(start, Is.EqualTo(new RunStart("20261123-063633-guide", null)));
    }

    /// <summary>A start the API refuses answers no run and the API's own reason, word for word.</summary>
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.NotFound)]
    public async Task ARefusedStart_AnswersTheApisReason(HttpStatusCode status)
    {
        // The client reads only the refusal's words, so its type is any.
        var refusal = new ApiError("error", new ApiErrorDetail("a-refusal", "A run is already going."));
        var (api, _) = Api(_ => new HttpResponseMessage(status)
        {
            Content = JsonContent.Create(refusal, options: JsonSerializerOptions.Web)
        });

        var start = await api.StartAsync("guide", RunKind.Replay, 1);

        Assert.That(start, Is.EqualTo(new RunStart(null, "A run is already going.")));
    }

    /// <summary>A stop sends DELETE to the run's address; a run not there, or not going, throws nothing.</summary>
    [TestCase(HttpStatusCode.NoContent)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Conflict)]
    public async Task AStop_SendsDeleteToTheRun_AndAMissOrAnEndedRunThrowsNothing(HttpStatusCode status)
    {
        var (api, stub) = Api(_ => Status(status));

        await api.StopAsync("20261123-063633-guide");

        Assert.That(stub.Requests.Single().Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(stub.Requests.Single().Url,
            Is.EqualTo("http://localhost/v1/admin/evals/runs/20261123-063633-guide"));
    }

    /// <summary>A refused key throws on a start and on a stop, as on every other call.</summary>
    [Test]
    public void ARefusedKey_ThrowsOnAStartAndOnAStop()
    {
        var (api, _) = Api(_ => Status(HttpStatusCode.Unauthorized));

        Assert.ThrowsAsync<AgentApiException>(() => api.StartAsync("guide", RunKind.Live, 1));
        Assert.ThrowsAsync<AgentApiException>(() => api.StopAsync("20261123-063633-guide"));
    }

    /// <summary>
    /// F126.4: a flip puts the note to the mark's own address, written as the routes read it, and an undo deletes
    /// there; both answer no refusal when the API says yes.
    /// </summary>
    [Test]
    public async Task AFlipAndAnUndo_CallTheMarksAddress()
    {
        var (api, stub) = Api(_ => Status(HttpStatusCode.NoContent));
        var place = new MarkPlace("20261123-063633-guide", "guide-what-is", 2, 1, 3);

        var flipped = await api.FlipAsync(place, "It does say so.");
        var undone = await api.UndoFlipAsync(place);

        const string address =
            "http://localhost/v1/admin/evals/runs/20261123-063633-guide/guide-what-is/2/1/criteria/3/flip";
        Assert.That(stub.Requests.Select(request => (request.Method, request.Url)),
            Is.EqualTo(new[] { (HttpMethod.Put, address), (HttpMethod.Delete, address) }));
        Assert.That(JsonDocument.Parse(stub.Requests[0].Body!).RootElement.GetProperty("note").GetString(),
            Is.EqualTo("It does say so."));
        Assert.That((flipped, undone), Is.EqualTo(((string?)null, (string?)null)));
    }

    /// <summary>A flip or an undo the API refuses answers the API's own reason, word for word.</summary>
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.NotFound)]
    public async Task ARefusedFlip_AnswersTheApisReason(HttpStatusCode status)
    {
        var refusal = new ApiError("error", new ApiErrorDetail("a-refusal", "The run is going."));
        var (api, _) = Api(_ => new HttpResponseMessage(status)
        {
            Content = JsonContent.Create(refusal, options: JsonSerializerOptions.Web)
        });
        var place = new MarkPlace("20261123-063633-guide", "guide-what-is", 1, 1, 1);

        Assert.That(await api.FlipAsync(place, null), Is.EqualTo("The run is going."));
        Assert.That(await api.UndoFlipAsync(place), Is.EqualTo("The run is going."));
    }

    /// <summary>
    /// F126.4: a re-grade posts to the run's re-grade address and answers the new run's id, or the API's reason when
    /// it refused.
    /// </summary>
    [Test]
    public async Task ARegrade_PostsToTheRunsRegradeAddress_AndAnswersTheNewRun_OrTheRefusal()
    {
        var (api, stub) = Api(_ => new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Headers = { Location = new Uri("/v1/admin/evals/runs/20261123-170000-guide", UriKind.Relative) }
        });
        var refusal = new ApiError("error", new ApiErrorDetail("a-refusal", "A run is already going."));
        var (refusing, _) = Api(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(refusal, options: JsonSerializerOptions.Web)
        });

        var started = await api.RegradeAsync("20261123-063633-guide");
        var refused = await refusing.RegradeAsync("20261123-063633-guide");

        Assert.That((stub.Requests.Single().Method, stub.Requests.Single().Url), Is.EqualTo((HttpMethod.Post,
            "http://localhost/v1/admin/evals/runs/20261123-063633-guide/regrade")));
        Assert.That(started, Is.EqualTo(new RunStart("20261123-170000-guide", null)));
        Assert.That(refused, Is.EqualTo(new RunStart(null, "A run is already going.")));
    }

    /// <summary>The client over a stub of the API.</summary>
    private static (EvalsApi Api, StubHandler Stub) Api(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var stub = new StubHandler(answer);
        var http = new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") };
        return (new EvalsApi(new AgentApi(http, new ChatSettings())), stub);
    }

    /// <summary>An answer of this value, written as the routes write it.</summary>
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: RunJson.Options) };

    /// <summary>An empty answer with this status.</summary>
    private static HttpResponseMessage Status(HttpStatusCode status) => new(status);

    /// <summary>A finished run with every field filled, a null cost and an enum among them.</summary>
    private static EvalRun Run()
    {
        var call = new ToolCallTranscript("ReadDoc", """{"path":"README.md"}""", "# louis-agent", Failed: false);
        var transcript = new TurnTranscript("What is it?", "A framework.", [call], TimeSpan.FromSeconds(6));
        var turn = new TurnRun(transcript, [new Mark("no-error", true, "No error.")],
            [new Mark("Names the doc", false, "It doesn't.")], null, null);
        return new EvalRun("20261123-063633-guide", "guide", "louis-agent-guide", RunKind.Live, 1, Started,
            [new ConversationRun("guide-what-is", 1, [new RepeatRun(1, [turn])])])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddSeconds(33)
        };
    }
}
