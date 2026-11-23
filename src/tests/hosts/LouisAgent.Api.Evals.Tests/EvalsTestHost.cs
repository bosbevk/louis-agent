using System.Net.Http.Json;
using LouisAgent.Api.Testing;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Runs;
using LouisAgent.Evals.Runs.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Evals.Tests;

/// <summary>
/// The API in memory with the evals mapped beside it, as a site's program maps them: a test identity whose folder
/// holds an eval of one conversation, a model for the agent and one for the judge, and a data folder and a
/// recordings folder of its own. A run reaches the agent at the test server, the program's own address in a test.
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
    private readonly bool _ownsFolders;

    private EvalsTestHost(WebApplication app, Folders folders, bool ownsFolders)
    {
        _app = app;
        _ownsFolders = ownsFolders;
        Kept = folders;
        Client = app.GetTestClient();
        Client.DefaultRequestHeaders.Add("x-api-key", AdminKey);
    }

    /// <summary>A client of the API, with the admin's key on every request.</summary>
    public HttpClient Client { get; }

    /// <summary>Where this host keeps its runs and its recordings.</summary>
    public Folders Kept { get; }

    /// <summary>The runs' folder under the data folder.</summary>
    public string RunsFolder => Path.Combine(Kept.Data!, "evals", "runs");

    /// <summary>
    /// The API started with the evals: a data folder and a recordings folder unless a test says there is none, the
    /// eval's one turn with this criterion when one is given, and the folders of an earlier host when one is named.
    /// </summary>
    public static async Task<EvalsTestHost> StartAsync(IChatClient agentModel, IChatClient? judgeModel = null,
        bool withDataFolder = true, bool withRecordings = true, string? criterion = null,
        EvalsTestHost? sameFoldersAs = null)
    {
        var folders = sameFoldersAs?.Kept ?? new Folders(
            withDataFolder ? Directory.CreateTempSubdirectory("evals-data-").FullName : null,
            withRecordings ? Directory.CreateTempSubdirectory("evals-recordings-").FullName : null);
        var judge = judgeModel ?? new FakeChatClient();
        var replaying = folders.Recordings is null
            ? judge
            : new JudgeReplay(judge, new ReplayStore(folders.Recordings));
        var options = new EvalsOptions(folders.Data, null, null, null);
        var app = ApiTestHost.Build(adminKey: AdminKey, model: agentModel, moreIdentities: [Identity(criterion)],
            services: (builder, host) => builder.AddLouisAgentEvals(host, replaying, options,
                services => ((TestServer)services.GetRequiredService<IServer>()).CreateClient()),
            routes: api => api.MapLouisAgentEvals(AdminKey), replayDirectory: folders.Recordings);
        await app.StartAsync();
        return new EvalsTestHost(app, folders, ownsFolders: sameFoldersAs is null);
    }

    /// <summary>Starts a run of the eval, live with one repeat unless a test says otherwise.</summary>
    public Task<HttpResponseMessage> StartRunAsync(int repeats = 1, RunKind kind = RunKind.Live)
    {
        var request = new StartRunRequest(kind, repeats);
        return Client.PostAsJsonAsync($"/v1/admin/evals/{Eval}/runs", request, RunJson.Options);
    }

    /// <summary>A run started and read until it has ended; the start must be accepted.</summary>
    public async Task<EvalRun> RunToTheEndAsync(RunKind kind = RunKind.Live)
    {
        using var started = await StartRunAsync(kind: kind);
        Assert.That(started.IsSuccessStatusCode, Is.True, await started.Content.ReadAsStringAsync());
        return await EndedAsync(started.Headers.Location!);
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

    /// <summary>The server goes, and the folders with it when this host made them.</summary>
    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        if (!_ownsFolders) return;

        foreach (string? folder in new[] { Kept.Data, Kept.Recordings })
            if (folder is not null && Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
    }

    /// <summary>A model that answers every call with this text, counting the calls in this box.</summary>
    public static FakeChatClient Answering(string text, Count? count = null) =>
        new(Enumerable.Repeat<Func<IList<ChatMessage>, ChatResponse>>(_ =>
        {
            if (count is not null) count.Calls++;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
        }, 20).ToArray());

    /// <summary>The test identity, its voice and its one eval in its folder.</summary>
    private static FakeIdentity Identity(string? criterion)
    {
        string criteria = criterion is null ? "" : $$""","criteria":["{{criterion}}"]""";
        return new FakeIdentity(Agent, provider: new FakePrompts(new Dictionary<string, string>
        {
            [$"agents/{Agent}/voice.md"] = "# An eval agent\n\nYou greet people.\n",
            [$"agents/{Agent}/{Eval}.eval.json"] =
                $$"""{"agent":"{{Agent}}","conversations":[{"id":"hello","turns":""" +
                $$"""[{"ask":"Hello?","facts":["no-error"]{{criteria}}}]}]}"""
        }));
    }

    /// <summary>Where a host keeps its runs and its recordings, or none.</summary>
    public sealed record Folders(string? Data, string? Recordings);

    /// <summary>How many times a model was called.</summary>
    public sealed class Count
    {
        /// <summary>The calls so far.</summary>
        public int Calls { get; set; }
    }
}
