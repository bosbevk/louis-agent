using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Studio;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Evals.Studio;

/// <summary>
/// <c>POST /v1/admin/evals/drafts/{id}/verdict</c>: the draft is written out as a folder and the runner's
/// <c>--check</c> judges it; the page gets whether it was ok and every problem in the runner's words. A draft that
/// isn't there is a 404; a runner that couldn't look is a 503 with its reason. The runner here is a stand-in that
/// answers with the lines a real one writes.
/// </summary>
public class DraftCheckEndpointTests
{
    private const string AdminKey = "admin-secret";
    private const string File = """{"name":"weather-eval","identity":"weather-agent"}""";

    /// <summary>The folders the stand-in runner was asked to check.</summary>
    private readonly List<string> _checked = [];

    /// <summary>NUnit keeps one instance for the fixture, so each test starts with no folder checked.</summary>
    [SetUp]
    public void ForgetTheFolders() => _checked.Clear();

    /// <summary>Starts the API with a stand-in runner that answers so.</summary>
    private async Task<(WebApplication App, HttpClient Client)> StartAsync(RunnerOutput answer)
    {
        var checker = new DraftChecker(folder =>
        {
            _checked.Add(folder);
            return answer;
        });
        var app = ApiTestHost.Build(adminKey: AdminKey, drafts: new InMemoryEvalDraftStore(), checker: checker);
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

    /// <summary>Makes a draft and gives its id.</summary>
    private static async Task<string> MakeAsync(HttpClient client)
    {
        var made = await Send(client, HttpMethod.Post, "/v1/admin/evals/drafts", new
        {
            json = File,
            author = "Louis"
        });
        return JsonDocument.Parse(await made.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString()!;
    }

    /// <summary>
    /// The runner's problems come back in its words, with ok false, and it judged the draft's folder.
    /// </summary>
    [Test]
    public async Task TheRunnersProblems_ComeBack_InItsWords()
    {
        var (app, client) = await StartAsync(new RunnerOutput(
            """
            {"problem":"Unknown check type 'within-tme'."}
            {"ok":false}
            """, null));
        await using var _ = app;
        string id = await MakeAsync(client);

        var reply = JsonDocument.Parse(await (await Send(client, HttpMethod.Post,
            $"/v1/admin/evals/drafts/{id}/verdict")).Content.ReadAsStringAsync()).RootElement;

        Assert.That((reply.GetProperty("ok").GetBoolean(), reply.GetProperty("problems")[0].GetString()),
            Is.EqualTo((false, "Unknown check type 'within-tme'.")));
        Assert.That(System.IO.File.ReadAllText(Path.Combine(_checked.Single(), "eval.json")), Is.EqualTo(File));
    }

    /// <summary>A folder with no problems is ok.</summary>
    [Test]
    public async Task AFolderWithNoProblems_IsOk()
    {
        var (app, client) = await StartAsync(new RunnerOutput("{\"ok\":true}\n", null));
        await using var _ = app;
        string id = await MakeAsync(client);

        var reply = JsonDocument.Parse(await (await Send(client, HttpMethod.Post,
            $"/v1/admin/evals/drafts/{id}/verdict")).Content.ReadAsStringAsync()).RootElement;

        Assert.That((reply.GetProperty("ok").GetBoolean(), reply.GetProperty("problems").GetArrayLength()),
            Is.EqualTo((true, 0)));
    }

    /// <summary>A draft that isn't there is a 404; a runner that couldn't look is a 503 with its reason.</summary>
    [Test]
    public async Task NoDraft_IsA404_AndARunnerThatCouldNotLook_IsA503()
    {
        var (app, client) = await StartAsync(new RunnerOutput("", "the eval runner isn't at /nope"));
        await using var _ = app;
        string id = await MakeAsync(client);

        var missing = await Send(client, HttpMethod.Post, "/v1/admin/evals/drafts/nope/verdict");
        var down = await Send(client, HttpMethod.Post, $"/v1/admin/evals/drafts/{id}/verdict");

        Assert.That((missing.StatusCode, down.StatusCode),
            Is.EqualTo((HttpStatusCode.NotFound, HttpStatusCode.ServiceUnavailable)));
        Assert.That(await down.Content.ReadAsStringAsync(), Does.Contain("isn't at /nope"));
    }
}
