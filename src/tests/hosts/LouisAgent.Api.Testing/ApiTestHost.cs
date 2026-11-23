using Microsoft.Extensions.DependencyInjection;
using LouisAgent.Core.Web;
using LouisAgent.Api.Endpoints.Admin;
using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Composition.Packs;
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

namespace LouisAgent.Api.Testing;

/// <summary>
/// The API as `ApiStartup` builds it, but in memory on a test server with no port: the same key gate, routes and
/// admin parts. The agent behind it never calls a model unless the test gives one, and the admin parts have no admin
/// key unless the test gives one.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class ApiTestHost
{
    /// <summary>The repository's folder, found from where the tests run.</summary>
    public static string RepoRoot { get; } =
        TestPaths.RepoRoot;

    /// <summary>
    /// The skills folder the test agent writes to, an empty one of the tests' own (F166.1: not a product's), made once
    /// for the run.
    /// </summary>
    private static string SkillsFolder { get; } = Directory.CreateTempSubdirectory("api-test-skills-").FullName;

    /// <summary>
    /// Starts the API in memory the way ApiStartup builds it, on a test server with no port. Give it an admin key to
    /// open the admin portal. A test that writes or commits gives its own workspace folder, and one that sends a
    /// message gives the model that answers it. A library a site's API maps beside the API's own adds its services
    /// and its routes on the API's group, as the site's program does.
    /// </summary>
    public static WebApplication Build(string? apiKey = null, string? workspaceRoot = null, string? adminKey = null,
        IChatClient? model = null, IReadOnlyList<IAgentPack>? morePacks = null,
        IReadOnlyList<IAgentIdentity>? moreIdentities = null, OpenDoorOptions? openDoor = null,
        IReadOnlyDictionary<string, Uri>? agentPeers = null, bool streamPace = false,
        Action<WebApplicationBuilder, AgentHostContext>? services = null, Action<IEndpointRouteBuilder>? routes = null)
    {
        // The same services as ApiStartup, on an in-memory test server instead of a port.
        var host = HostContext(workspaceRoot ?? RepoRoot, model ?? new NoModel(), morePacks, moreIdentities,
            agentPeers, streamPace);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddApi(host, openDoor ?? OpenDoorOptions.Shut);
        services?.Invoke(builder, host);
        var app = builder.Build();

        if (apiKey is not null) app.UseAgentKey(apiKey, app.Services.GetRequiredService<OpenDoor>());
        var api = app.MapApi(host);
        api.MapAdmin(adminKey);
        routes?.Invoke(api);
        return app;
    }

    /// <summary>Every route the API maps, endpoint by endpoint.</summary>
    public static IReadOnlyList<RouteEndpoint> ListRoutes(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
        .ToList();

    /// <summary>
    /// The route's pattern with its group prefix and constraints (<c>/v1/admin/sessions/{id}/mine</c>); every route's
    /// starts with a slash.
    /// </summary>
    public static string ReadPattern(RouteEndpoint endpoint)
    {
        string text = endpoint.RoutePattern.RawText ?? "";
        Assert.That(text, Does.StartWith("/"), "a route's raw pattern names its whole path, group prefix included");
        return text;
    }

    /// <summary>
    /// The route as <c>METHOD /pattern</c>: its methods joined with commas, or <c>ANY</c> for a route that takes every
    /// method.
    /// </summary>
    public static string DescribeRoute(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        string method = methods is { Count: > 0 } ? string.Join(",", methods) : "ANY";
        return $"{method} {ReadPattern(endpoint)}";
    }

    /// <summary>Who may call the route, as the key gate reads it.</summary>
    public static RouteAccess ReadAccess(RouteEndpoint endpoint) =>
        AgentKeyGate.AccessFor(new PathString(ReadPattern(endpoint)), endpoint);

    /// <summary>An agent with no model, for a test about how the API starts up.</summary>
    public static AgentHostContext BareHost() => HostContext(RepoRoot, new NoModel());

    /// <summary>
    /// The agent the test API runs: the test agent and any identity a test adds (F166.1: no product), on the test
    /// agent's packs, the tests' own tools and any a test adds, with this workspace and model, the peers a test names
    /// (none by default), and its words paced when a test says so (F156).
    /// </summary>
    private static AgentHostContext HostContext(string workspaceRoot, IChatClient model,
        IReadOnlyList<IAgentPack>? morePacks = null, IReadOnlyList<IAgentIdentity>? moreIdentities = null,
        IReadOnlyDictionary<string, Uri>? agentPeers = null, bool streamPace = false)
    {
        var options = new AgentOptions
        {
            WorkspaceRoot = workspaceRoot,
            SkillsDirectory = SkillsFolder,
            RatesUrl = null,
            AgentPeers = agentPeers ?? new Dictionary<string, Uri>(),
            StreamPace = streamPace
        };
        // A test may add an identity beside the test agent (F109.3: a session names its identity), and its packs.
        var catalog = new AgentCatalog([TestAgent.Identity, .. moreIdentities ?? []],
            [.. TestAgent.Packs, TestTools.Pack, .. morePacks ?? []]);
        var engines = new AgentEngineFactory(model, options, services: new HostServices(WebAccess.Create),
            catalog: catalog);
        var engine = engines.GetOrCreate(engines.Available);
        return new AgentHostContext(engine, new LlmOptions(), options, PriceTable.Empty, ExchangeRates.Empty,
            DisplayCurrency.Charged("USD"), engines);
    }

    /// <summary>A chat client that is never called: a route test that sends no message gets it.</summary>
    private sealed class NoModel : IChatClient
    {
        /// <summary>Never called: no route test sends a message.</summary>
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        /// <summary>Never called: no route test streams a message.</summary>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <summary>Has no services to offer.</summary>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        /// <summary>Nothing to clean up.</summary>
        public void Dispose()
        {
        }
    }
}
