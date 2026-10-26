using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The API as <c>Program.cs</c> builds it, in memory (CL20 step 1, ADR 0039): the same <c>AddApi</c>, the same key gate and the same
/// <c>MapApi</c> and <c>MapAdmin</c>, on a test server with no port. The agent behind it needs no network and no model call: its
/// engine is built on a client that is never called, as <c>AgentSessionsTests</c> builds its sessions. The admin parts are built as
/// <c>Program.cs</c> builds them, with no store and no admin key unless the test gives them: the default checks, no recorder.
/// </summary>
internal static class ApiTestHost
{
    public static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    /// <summary>
    /// The API with every route mapped; <paramref name="apiKey"/> adds the key gate, as a key set in the environment does, and
    /// <paramref name="adminKey"/> and <paramref name="store"/> open the admin portal and its benchmark store (none: closed, no store; F18 step 5.2), and a test of the runs gives its own <paramref name="catalog"/>, <paramref name="runner"/> and <paramref name="recorder"/> (F18 step 5b.2),
    /// and <paramref name="workspaceRoot"/> is the folder the agent works in (this repository when none: a test that writes or commits
    /// names a folder of its own).
    /// </summary>
    public static WebApplication Build(string? apiKey = null, string? workspaceRoot = null, string? adminKey = null, IBenchmarkStore? store = null,
        BenchmarkRunner? runner = null, BenchmarkRecorder? recorder = null, BenchmarkCatalog? catalog = null, IHumanReviewStore? reviews = null)
    {
        var host = HostContext(workspaceRoot ?? RepoRoot);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddApi(host);
        var app = builder.Build();

        if (apiKey is not null) app.UseAgentKey(apiKey);
        var api = app.MapApi(host);

        var benchmarks = catalog ?? new BenchmarkCatalog(BenchmarkCatalog.Default);
        api.MapAdmin(benchmarks, runner ?? new BenchmarkRunner(benchmarks, host.Engine.WorkspaceRoot, "http://127.0.0.1:1/"), adminKey, store, recorder,
            _ => new Dictionary<string, string>(), host.Engine.WorkspaceRoot, reviews);
        return app;
    }

    /// <summary>Every route the API maps, endpoint by endpoint.</summary>
    public static IReadOnlyList<RouteEndpoint> Routes(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToList();

    /// <summary>The route's pattern with its group prefix and constraints (<c>/v1/admin/benchmarks/runs/{id}</c>); every route's starts with a slash.</summary>
    public static string Pattern(RouteEndpoint endpoint)
    {
        string text = endpoint.RoutePattern.RawText ?? "";
        Assert.That(text, Does.StartWith("/"), "a route's raw pattern names its whole path, group prefix included");
        return text;
    }

    /// <summary>The route as <c>METHOD /pattern</c>: its methods joined with commas, or <c>ANY</c> for a route that takes every method.</summary>
    public static string Line(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        string method = methods is { Count: > 0 } ? string.Join(",", methods) : "ANY";
        return $"{method} {Pattern(endpoint)}";
    }

    /// <summary>Who may call the route, as the key gate reads it.</summary>
    public static RouteAccess Access(RouteEndpoint endpoint) => AgentKeyGate.AccessFor(new PathString(Pattern(endpoint)), endpoint);

    private static AgentHostContext HostContext(string workspaceRoot)
    {
        var options = new AgentOptions { WorkspaceRoot = workspaceRoot, SkillsDirectory = Path.Combine(RepoRoot, "Skills"), RatesUrl = null };
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
