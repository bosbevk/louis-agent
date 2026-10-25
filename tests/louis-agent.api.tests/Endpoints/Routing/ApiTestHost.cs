using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The API as <c>Program.cs</c> builds it, in memory (CL20 step 1, ADR 0039): the same <c>AddApi</c>, the same key gate and the same
/// <c>MapApi</c> and <c>MapAdmin</c>, on a test server with no port. The agent behind it needs no network and no model call: its
/// engine is built on a client that is never called, as <c>AgentSessionsTests</c> builds its sessions. The admin parts are built as
/// <c>Program.cs</c> builds them with no store: the default checks, no admin key, no recorder.
/// </summary>
internal static class ApiTestHost
{
    public static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    /// <summary>The API with every route mapped; <paramref name="apiKey"/> adds the key gate, as a key set in the environment does.</summary>
    public static WebApplication Build(string? apiKey = null)
    {
        var host = HostContext();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddApi(host);
        var app = builder.Build();

        if (apiKey is not null) app.UseAgentKey(apiKey);
        var api = app.MapApi(host);

        var benchmarks = new BenchmarkCatalog(BenchmarkCatalog.Default);
        api.MapAdmin(benchmarks, new BenchmarkRunner(benchmarks, host.Engine.WorkspaceRoot, "http://127.0.0.1:1/"), null, null, null,
            _ => new Dictionary<string, string>(), host.Engine.WorkspaceRoot);
        return app;
    }

    /// <summary>Every route the API maps, endpoint by endpoint.</summary>
    public static IReadOnlyList<RouteEndpoint> Routes(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToList();

    private static AgentHostContext HostContext()
    {
        var options = new AgentOptions { WorkspaceRoot = RepoRoot, SkillsDirectory = Path.Combine(RepoRoot, "Skills"), RatesUrl = null };
        var catalog = new AgentCatalog([new LouisAgent.Identities.Puls.PulsIdentity()],
            [new LouisAgent.Packs.Shops.ShopsPack(new LouisAgent.Packs.Shops.ShopsOptions())]);
        var engines = new AgentEngineFactory(new NoModel(), options, catalog: catalog);
        var engine = engines.GetOrCreate(engines.Available);
        return new AgentHostContext(engine, new LlmOptions(), options, PriceTable.Empty, ExchangeRates.Empty, DisplayCurrency.Charged("USD"), engines);
    }

    /// <summary>A chat client that is never called: no route test sends a message.</summary>
    private sealed class NoModel : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
