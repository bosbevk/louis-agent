using Microsoft.Extensions.DependencyInjection;
using LouisAgent.Core.Web;
using LouisAgent.Packs.Web;
using LouisAgent.Api.Endpoints.Admin;
using LouisAgent.Api.Evals.Endpoints.Runs;
using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Identities.Puls;
using LouisAgent.Packs.Calculator;
using LouisAgent.Packs.Recipes;
using LouisAgent.Packs.ShopPrices;
using LouisAgent.Packs.WhereToBuy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Testing;

/// <summary>
/// The API as `ApiStartup` builds it, but in memory on a test server with no port: the same key gate, routes and
/// admin parts. The agent behind it never calls a model, and the admin parts have no store and no admin key unless the
/// test gives them.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class ApiTestHost
{
    /// <summary>The repository's folder, found from where the tests run.</summary>
    public static string RepoRoot { get; } =
        TestPaths.RepoRoot;

    /// <summary>
    /// Starts the API in memory the way ApiStartup builds it, on a test server with no port. Give it an admin key and a
    /// store to open the admin portal, and a clock to fix the time notes are stamped with. A test that writes or
    /// commits gives its own workspace folder, and one that sends a message gives the model that answers it.
    /// </summary>
    public static WebApplication Build(string? apiKey = null, string? workspaceRoot = null, string? adminKey = null,
        IEvalStore? store = null,
        EvalProcess? runner = null, EvalRecorder? recorder = null, EvalList? catalog = null,
        IHumanReviewStore? reviews = null,
        ICalibrationStore? calibrations = null, CalibrationRecorder? calibrationRecorder = null,
        TimeProvider? time = null, IEvalDraftStore? drafts = null, CheckList? checks = null,
        DraftChecker? checker = null, PromptfooRunner? promptfoo = null, IChatClient? model = null,
        IReadOnlyList<IAgentPack>? morePacks = null, bool evals = true,
        IReadOnlyList<IAgentIdentity>? moreIdentities = null, OpenDoorOptions? openDoor = null,
        IReadOnlyDictionary<string, Uri>? agentPeers = null)
    {
        // The same services as ApiStartup, on an in-memory test server instead of a port.
        var host = HostContext(workspaceRoot ?? RepoRoot, model ?? new NoModel(), morePacks, moreIdentities,
            agentPeers);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddApi(host, openDoor ?? OpenDoorOptions.Shut);
        var app = builder.Build();

        if (apiKey is not null) app.UseAgentKey(apiKey, app.Services.GetRequiredService<OpenDoor>());
        var api = app.MapApi(host);
        api.MapAdmin(adminKey);
        if (!evals) return app;

        // The admin parts, with whatever the test handed in. The runner points at a port nothing listens on, so a
        // test that forgets to give its own can't reach a real agent.
        var benchmarks = catalog ?? new EvalList();
        api.MapAdminGroup(adminKey).MapEvalAdmin(benchmarks,
            runner ?? new EvalProcess(benchmarks, host.Engine.WorkspaceRoot, "http://127.0.0.1:1/"),
            store, recorder,
            _ => new Dictionary<string, string>(), host.Engine.WorkspaceRoot, reviews, calibrations,
            calibrationRecorder, time, drafts ?? new InMemoryEvalDraftStore(),
            new StudioRunner(checks ?? StudioRunner.None.Checks, checker ?? StudioRunner.None.Checker,
                promptfoo ?? StudioRunner.None.Promptfoo));
        return app;
    }

    /// <summary>Every route the API maps, endpoint by endpoint.</summary>
    public static IReadOnlyList<RouteEndpoint> Routes(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
        .ToList();

    /// <summary>
    /// The route's pattern with its group prefix and constraints (<c>/v1/admin/benchmarks/runs/{id}</c>); every route's
    /// starts with a slash.
    /// </summary>
    public static string Pattern(RouteEndpoint endpoint)
    {
        string text = endpoint.RoutePattern.RawText ?? "";
        Assert.That(text, Does.StartWith("/"), "a route's raw pattern names its whole path, group prefix included");
        return text;
    }

    /// <summary>
    /// The route as <c>METHOD /pattern</c>: its methods joined with commas, or <c>ANY</c> for a route that takes every
    /// method.
    /// </summary>
    public static string Line(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        string method = methods is { Count: > 0 } ? string.Join(",", methods) : "ANY";
        return $"{method} {Pattern(endpoint)}";
    }

    /// <summary>Who may call the route, as the key gate reads it.</summary>
    public static RouteAccess Access(RouteEndpoint endpoint) =>
        AgentKeyGate.AccessFor(new PathString(Pattern(endpoint)), endpoint);

    /// <summary>
    /// What the API runs on: an engine over the model given (one that is never called, unless a test sends a message),
    /// with the Puls identity and the packs it names.
    /// </summary>
    /// <summary>An agent with no model, for a test about how the API starts up.</summary>
    public static AgentHostContext BareHost() => HostContext(RepoRoot, new NoModel());

    private static AgentHostContext HostContext(string workspaceRoot, IChatClient model,
        IReadOnlyList<IAgentPack>? morePacks = null, IReadOnlyList<IAgentIdentity>? moreIdentities = null,
        IReadOnlyDictionary<string, Uri>? agentPeers = null)
    {
        var options = new AgentOptions
        {
            WorkspaceRoot = workspaceRoot,
            SkillsDirectory = Path.Combine(RepoRoot, "demos/puls/skills"),
            RatesUrl = null,
            AgentPeers = agentPeers ?? new Dictionary<string, Uri>()
        };
        // Puls names the recipes pack too (F35.2 step 1), so the catalogue holds it, with its settings unset.
        // A test may add an identity beside Puls's (F109.3: a session names its identity).
        var catalog = new AgentCatalog([new PulsIdentity(), .. moreIdentities ?? []],
        [
            new WebPack(), TestTools.Pack, new WhereToBuyPack(), new ShopPricesPack(new ShopPricesOptions()),
            new RecipesPack(new RecipesOptions()), new CalculatorPack(), .. morePacks ?? []
        ]);
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
