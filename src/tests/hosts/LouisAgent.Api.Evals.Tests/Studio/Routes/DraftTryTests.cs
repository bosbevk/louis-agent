using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Start;
using LouisAgent.Api.Evals.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Evals.Tests.Evals.Studio.Routes;

/// <summary>
/// A studio try (F49.5): a replay of a stored run, on the draft's folder and a few of its samples, so it costs nothing.
/// The runner is started as for a replay, with <c>--folder</c> and <c>--samples</c> after it; the route refuses a try
/// with no replay or with samples that aren't ids, and a draft that isn't there.
/// </summary>
public class DraftTryTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>A try's run starts the runner as a replay, the folder and the samples after it.</summary>
    [Test]
    public void ATry_StartsTheRunner_AsAReplay_OnTheFolderAndSamples()
    {
        var eval = new EvalEntry("weather-eval", "Weather");
        var runner = new EvalProcess(new EvalList([]), Path.GetTempPath(), "http://127.0.0.1:1/", "/app/evals/x.dll");
        var options = new EvalRunOptions(Replay: "run_7", Folder: "/tmp/studio/d1",
            Samples: "weather-paarl,weather-af");

        var start = runner.StartInfoFor(eval, new RunningEval("run_1", "weather-eval", 1, options));

        Assert.That(start.ArgumentList, Is.EqualTo(new[]
        {
            "/app/evals/x.dll", "run", "weather-eval", "--repeat", "1", "--replay", "run_7",
            "--folder", "/tmp/studio/d1", "--samples", "weather-paarl,weather-af"
        }));
    }

    /// <summary>Starts the API with one eval draft and a store, and gives its id.</summary>
    private static async Task<(WebApplication App, HttpClient Client, string Id)> StartAsync()
    {
        var app = ApiTestHost.Build(adminKey: AdminKey, store: new InMemoryEvalStore(),
            drafts: new InMemoryEvalDraftStore());
        await app.StartAsync();
        var client = app.GetTestClient();
        var made = await Send(client, HttpMethod.Post, "/v1/admin/evals/drafts", new
        {
            json = """{"name":"weather-eval","identity":"weather-agent"}""",
            author = "Louis"
        });
        string id = JsonDocument.Parse(await made.Content.ReadAsStringAsync()).RootElement.GetProperty("id")
            .GetString()!;
        return (app, client, id);
    }

    /// <summary>Sends a request with the admin key, and any body as JSON.</summary>
    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("x-api-key", AdminKey);
        if (body is not null) request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }

    /// <summary>A try with no replay, or samples that aren't ids, is a 400; a draft that isn't there a 404.</summary>
    [Test]
    public async Task ABadTry_IsRefused()
    {
        var (app, client, id) = await StartAsync();
        await using var _ = app;

        var noReplay = await Send(client, HttpMethod.Post, $"/v1/admin/evals/drafts/{id}/try", new
        {
            samples = new[] { "weather-paarl" }
        });
        var badSamples = await Send(client, HttpMethod.Post, $"/v1/admin/evals/drafts/{id}/try", new
        {
            replay = "run_7",
            samples = new[] { "Weather Paarl" }
        });
        var noDraft = await Send(client, HttpMethod.Post, "/v1/admin/evals/drafts/nope/try", new
        {
            replay = "run_7"
        });

        Assert.That((noReplay.StatusCode, badSamples.StatusCode, noDraft.StatusCode),
            Is.EqualTo((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.NotFound)));
    }
}
