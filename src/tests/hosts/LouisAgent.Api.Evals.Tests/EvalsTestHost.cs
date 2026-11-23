using System.Net.Http.Json;
using LouisAgent.Api.Testing;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Runs.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Evals.Tests;

/// <summary>
/// The API in memory with the evals mapped beside it, as a site's program maps them: a test identity whose folder
/// holds an eval of one conversation, a model for the agent and one for the judge, and a data folder of its own.
/// A run reaches the agent at the test server, the program's own address in a test.
/// </summary>
internal sealed class EvalsTestHost : IAsyncDisposable
{
    /// <summary>The admin key every eval route asks for.</summary>
    public const string AdminKey = "evals-admin-key";

    /// <summary>The test identity's name, which the eval proves.</summary>
    public const string Agent = "an-eval-agent";

    /// <summary>The eval's name, its file's name.</summary>
    public const string Eval = "greets";

    private readonly WebApplication _app;

    private EvalsTestHost(WebApplication app, string? dataFolder)
    {
        _app = app;
        DataFolder = dataFolder;
        Client = app.GetTestClient();
        Client.DefaultRequestHeaders.Add("x-api-key", AdminKey);
    }

    /// <summary>A client of the API, with the admin's key on every request.</summary>
    public HttpClient Client { get; }

    /// <summary>The data folder this host keeps its runs under, or none.</summary>
    public string? DataFolder { get; }

    /// <summary>The runs' folder under the data folder.</summary>
    public string RunsFolder => Path.Combine(DataFolder!, "evals", "runs");

    /// <summary>The API started with the evals, with a data folder unless a test says there is none.</summary>
    public static async Task<EvalsTestHost> StartAsync(IChatClient agentModel, IChatClient? judgeModel = null,
        bool withDataFolder = true)
    {
        string? data = withDataFolder ? Directory.CreateTempSubdirectory("evals-data-").FullName : null;
        var options = new EvalsOptions(data, null, null, null);
        var app = ApiTestHost.Build(adminKey: AdminKey, model: agentModel, moreIdentities: [Identity()],
            services: (builder, host) => builder.AddLouisAgentEvals(host, judgeModel ?? new FakeChatClient(), options,
                services => ((TestServer)services.GetRequiredService<IServer>()).CreateClient()),
            routes: api => api.MapLouisAgentEvals(AdminKey));
        await app.StartAsync();
        return new EvalsTestHost(app, data);
    }

    /// <summary>Starts a run of the eval, live with one repeat unless a test says otherwise.</summary>
    public Task<HttpResponseMessage> StartRunAsync(int repeats = 1, RunKind kind = RunKind.Live)
    {
        var request = new StartRunRequest(kind, repeats);
        return Client.PostAsJsonAsync($"/v1/admin/evals/{Eval}/runs", request, RunJson.Options);
    }

    /// <summary>The run at this address, read until it has ended.</summary>
    public async Task<EvalRun> EndedAsync(Uri location)
    {
        for (int tries = 0; tries < 500; tries++)
        {
            var run = await RunAtAsync(location);
            if (run.Status != RunStatus.Running) return run;
            await Task.Delay(20);
        }

        throw new TimeoutException($"The run at {location} never ended.");
    }

    /// <summary>The run at this address, as the API sends it.</summary>
    public async Task<EvalRun> RunAtAsync(Uri location) =>
        (await Client.GetFromJsonAsync<EvalRun>(location, RunJson.Options))!;

    /// <summary>The server and its data folder go.</summary>
    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        if (DataFolder is not null && Directory.Exists(DataFolder)) Directory.Delete(DataFolder, recursive: true);
    }

    /// <summary>A model for the agent that answers every turn with this text.</summary>
    public static FakeChatClient Answering(string text) =>
        new(Enumerable.Repeat<Func<IList<ChatMessage>, ChatResponse>>(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, text)), 20).ToArray());

    /// <summary>The test identity, its voice and its one eval in its folder.</summary>
    private static FakeIdentity Identity() => new(Agent, provider: new FakePrompts(new Dictionary<string, string>
    {
        [$"agents/{Agent}/voice.md"] = "# An eval agent\n\nYou greet people.\n",
        [$"agents/{Agent}/{Eval}.eval.json"] =
            $$"""{"agent":"{{Agent}}","conversations":[{"id":"hello","turns":""" +
            """[{"ask":"Hello?","facts":["no-error"]}]}]}"""
    }));
}
