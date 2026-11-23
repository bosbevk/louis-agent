using LouisAgent.Api.Endpoints.Admin;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Runs;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Runs.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Evals;

/// <summary>
/// How a site's API program takes the evals: their services beside the API's, and their four routes on the API's
/// group behind the admin key. The host's eval files are read here, so a wrong one stops it at start-up.
/// </summary>
/// <remarks>
/// The evals ask the agent over HTTP at the program's own address, with the instance's key, as any app does, so the
/// engine and the API hold no eval code and the agent measured is the one visitors get.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public static class EvalsStartup
{
    /// <summary>How long one ask may take before its answer starts, as long as a slow turn needs.</summary>
    private static readonly TimeSpan TurnLimit = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The evals' services: the host's eval files, the desk that runs one run at a time in the background, and the
    /// routes' handlers. A test gives its own client of the API; a site's program leaves it to the settings.
    /// </summary>
    public static WebApplicationBuilder AddLouisAgentEvals(this WebApplicationBuilder builder, AgentHostContext host,
        IChatClient judgeModel, EvalsOptions options, Func<IServiceProvider, HttpClient>? agentApi = null)
    {
        var catalog = EvalCatalog.Read(host.Engines.Catalog);
        var api = agentApi ?? (provider => OwnApi(provider, options));

        builder.Services.AddSingleton<RunDesk>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<RunDesk>());
        builder.Services.AddSingleton(provider =>
        {
            var work = options.RunsFolder is { } folder
                ? new RunWork(new RunStore(folder), judgeModel, () => api(provider), ReplayStore.From(host.Agent),
                    TimeProvider.System)
                : null;
            return new EvalHandlers(catalog, provider.GetRequiredService<RunDesk>(), work, host.Agent.KeepsRecordings,
                TimeProvider.System);
        });
        return builder;
    }

    /// <summary>The five routes, on the API's group, behind the admin key.</summary>
    public static IEndpointRouteBuilder MapLouisAgentEvals(this IEndpointRouteBuilder v1, string? adminKey)
    {
        var handlers = v1.ServiceProvider.GetRequiredService<EvalHandlers>();
        var admin = v1.MapAdminGroup(adminKey);
        admin.MapGet("/evals", handlers.List);
        admin.MapPost("/evals/{name}/runs", handlers.StartAsync);
        admin.MapGet("/evals/runs/{id}", handlers.Find);
        admin.MapDelete("/evals/runs/{id}", handlers.Stop);
        admin.MapGet("/evals/{name}/runs", handlers.RunsOf);
        return v1;
    }

    /// <summary>
    /// The judge's client: the host's provider on the judge's model, recorded and replayed with the agent's answers,
    /// its cost on the host's ledger when the host keeps one, under the grading purpose the judge sets; a replayed
    /// answer is a known 0.
    /// </summary>
    public static IChatClient JudgeClient(AgentHostContext host, ILlmClientFactory clients, EvalsOptions options)
    {
        var llm = options.JudgeLlm(host.Llm);
        // Its answers are kept in each play's trial and replayed from it, when the host keeps recordings.
        var client = ReplayStore.From(host.Agent) is { } recordings
            ? new JudgeReplay(clients.Create(llm), recordings)
            : clients.Create(llm);
        var ledger = UsageLedger.Open(host.Agent, host.Prices);
        return ledger is null
            ? client
            : new UsageRecordingChatClient(client, ledger, llm.Model, clients.CreateUsageMapper(llm));
    }

    /// <summary>A client of the API at the set address, or at the program's own, with the instance's key.</summary>
    private static HttpClient OwnApi(IServiceProvider provider, EvalsOptions options)
    {
        var http = new HttpClient
        {
            BaseAddress = options.ApiUrl ?? OwnAddress(provider.GetRequiredService<IServer>()),
            Timeout = TurnLimit
        };
        if (options.ApiKey is not null) http.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
        return http;
    }

    /// <summary>
    /// The program's own address on this machine: the port it listens on, at the loopback address, since it may listen
    /// on every address (<c>http://+:8080</c>), which is no address to call.
    /// </summary>
    private static Uri OwnAddress(IServer server)
    {
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        string listening = addresses.FirstOrDefault(address => address.StartsWith("http://", StringComparison.Ordinal))
                           ?? addresses.First();
        string scheme = listening[..listening.IndexOf("://", StringComparison.Ordinal)];
        string port = listening[(listening.LastIndexOf(':') + 1)..].TrimEnd('/');
        return new Uri($"{scheme}://127.0.0.1:{port}/");
    }
}
