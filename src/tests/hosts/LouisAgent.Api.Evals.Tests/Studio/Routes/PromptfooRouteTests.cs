using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Studio;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Evals.Tests.Evals.Studio.Routes;

/// <summary>
/// promptfoo on the studio's drafts (F49.5): a file is imported by the runner into a new eval draft, its file, its
/// rubrics and its scenarios kept, what has no counterpart named; a draft is exported by the runner as a promptfoo
/// file. The runner here is a stand-in that writes and prints what a real one does.
/// </summary>
public class PromptfooRouteTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>What the stand-in runner was asked, one argument list each.</summary>
    private readonly List<IReadOnlyList<string>> _asked = [];

    /// <summary>NUnit keeps one instance for the fixture, so each test starts with nothing asked.</summary>
    [SetUp]
    public void ForgetTheCalls() => _asked.Clear();

    /// <summary>
    /// A stand-in runner: an import writes the folder a real one would and leaves one assert out; an export prints
    /// a promptfoo file.
    /// </summary>
    private RunnerOutput Runner(IReadOnlyList<string> args)
    {
        _asked.Add(args);
        if (args[0] == "--export-promptfoo") return new RunnerOutput("description: Weather\ntests: []\n", null);

        string folder = args[2];
        Directory.CreateDirectory(Path.Combine(folder, "Rubrics"));
        Directory.CreateDirectory(Path.Combine(folder, "Dataset", "scenarios", "dev"));
        File.WriteAllText(Path.Combine(folder, "eval.json"), """{"name":"weather-eval","identity":"imported"}""");
        File.WriteAllText(Path.Combine(folder, "Rubrics", "rubric-1.md"), "Is die weer reg?");
        File.WriteAllText(Path.Combine(folder, "Dataset", "scenarios", "dev", "test-1.json"), """{"id":"test-1"}""");
        return new RunnerOutput("""{"left_out":"promptfoo's 'javascript' assert has no check of ours: left out."}""",
            null);
    }

    /// <summary>Starts the API with a draft store and the stand-in runner.</summary>
    private async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        var app = ApiTestHost.Build(adminKey: AdminKey, drafts: new InMemoryEvalDraftStore(),
            promptfoo: new PromptfooRunner(Runner));
        await app.StartAsync();
        return (app, app.GetTestClient());
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

    /// <summary>An import makes a draft with the file, rubrics and scenarios, and names what was left out.</summary>
    [Test]
    public async Task AnImport_MakesADraft_AndNamesWhatWasLeftOut()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await Send(client, HttpMethod.Post, "/v1/admin/evals/drafts/promptfoo", new
        {
            yaml = "description: Weather\n",
            author = "Louis"
        });

        string body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), body);
        var reply = JsonDocument.Parse(body).RootElement;
        var draft = reply.GetProperty("draft");
        Assert.That((draft.GetProperty("name").GetString(),
                draft.GetProperty("rubrics")[0].GetProperty("name").GetString(),
                draft.GetProperty("scenarios")[0].GetProperty("name").GetString()),
            Is.EqualTo(("weather-eval", "rubric-1.md", "test-1.json")));
        Assert.That(reply.GetProperty("left_out")[0].GetString(), Does.Contain("javascript"));
        Assert.That(_asked.Single()[0], Is.EqualTo("--import-promptfoo"));
    }

    /// <summary>An export hands back the runner's promptfoo file for the draft's folder.</summary>
    [Test]
    public async Task AnExport_HandsBackThePromptfooFile()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;
        var made = await Send(client, HttpMethod.Post, "/v1/admin/evals/drafts", new
        {
            json = """{"name":"weather-eval","identity":"weather-agent"}""",
            author = "Louis"
        });
        string id = JsonDocument.Parse(await made.Content.ReadAsStringAsync()).RootElement.GetProperty("id")
            .GetString()!;

        var exported = await Send(client, HttpMethod.Get, $"/v1/admin/evals/drafts/{id}/promptfoo");

        Assert.That(exported.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await exported.Content.ReadAsStringAsync(), Does.StartWith("description: Weather"));
        Assert.That(_asked.Single()[0], Is.EqualTo("--export-promptfoo"));
    }
}
