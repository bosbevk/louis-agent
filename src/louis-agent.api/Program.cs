using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions;
using LouisAgent.Composition;
using LouisAgent.Core.Guardrails.Auth;
using LouisAgent.Core.Guardrails.Workspace;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

// HTTP version of the ACP server, which also serves the web app: every API reply is JSON, and a message's answer streams as Server-Sent Events shaped
// like Claude's (message_start, content_block_start/delta/stop for thinking, text, tool_use and tool_result, message_stop).
// Start-up only: the routes are in Endpoints/, a turn's stream in Sessions/.
var host = AgentHost.Build(Plugins.FromEnvironment());

var builder = WebApplication.CreateBuilder(args);
// host.Engine (every toolset this instance can load) serves /workspace and /git; sessions get their own (F5).
builder.Services.AddSingleton(host.Engine);
builder.Services.AddSingleton(host.Engines);
builder.Services.AddSingleton<AgentSessions>();
builder.Services.AddSingleton(new WorkspaceBoundary(host.Engine.WorkspaceRoot));
var app = builder.Build();

// The agent can run shell commands and write files, so every /sessions, /workspace and /git call must present
// AGENT_API_KEY. FX18: without one the API refuses to start, here, before it listens, unless AGENT_AUTH=off says it is
// open on purpose (HostAuth, core's one rule for both this and the MCP server over HTTP).
// The web app's files stay public: they hold no data, and the app sends the key the user enters in its settings.
string? apiKey = HostAuth.Key(Environment.GetEnvironmentVariable("AGENT_API_KEY"));
switch (HostAuth.AtStartup(HostAuth.Mode(Environment.GetEnvironmentVariable("AGENT_AUTH")), apiKey))
{
    case HostAuthDecision.Refuse:
        throw new InvalidOperationException(MessageText.Format(ErrorMessage.ApiKeyRequired, new { Setting = "AGENT_API_KEY" }));
    case HostAuthDecision.StartOpen:
        AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api").Log(LogLevel.Warning, ApiLogMessage.NoApiKey);
        break;
    case HostAuthDecision.Start:
        app.Use(async (context, next) =>
        {
            bool isApi = context.Request.Path.StartsWithSegments("/sessions") || context.Request.Path.StartsWithSegments("/workspace") ||
                         context.Request.Path.StartsWithSegments("/git") || context.Request.Path.StartsWithSegments("/toolsets") ||
                         RolesView.Routes.Any(route => context.Request.Path.StartsWithSegments(route)) || context.Request.Path.StartsWithSegments("/model");
            if (!isApi || HostAuth.HasKey(context.Request.Headers["x-api-key"].FirstOrDefault(), context.Request.Headers.Authorization.FirstOrDefault(), apiKey))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiErrors.Body("authentication_error", MessageText.Format(ApiUserText.InvalidApiKey)));
        });
        break;
}

// The Blazor web app (louis-agent.web): MapStaticAssets serves its files (including _framework/, compressed), and any
// path that isn't an API route loads it, e.g. /chat/{sessionId}.
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.MapInfo(host);
app.MapWorkspace();
app.MapGit();
app.MapHistory();

// F27: the admin portal, behind its own key (ADMIN_API_KEY, else AGENT_API_KEY; neither: closed); its settings in
// AdminOptions (F31). A run's script calls this API back at BENCHMARK_API_URL (the container's own port, by default).
// F31: each run is recorded into the benchmark store (BENCHMARKS_MONGO_URL), its cost joined from this instance's
// ledger; without a store no run starts. At start-up nothing runs, so a run still "running" was cut off by a restart.
var admin = AdminOptions.FromEnvironment();
var benchmarkLog = AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api.Benchmarks");
// The checks: the C# evals the runner lists (read on first use), and the Python checks it doesn't list yet.
var benchmarks = BenchmarkCatalog.From(EvalListing.ForRunner(admin.EvalRunner), BenchmarkCatalog.Default, benchmarkLog);
IBenchmarkStore? benchmarkStore = admin.BenchmarksMongoUrl is { } mongoUrl ? new MongoBenchmarkStore(mongoUrl, admin.BenchmarksDatabase) : null;
BenchmarkRecorder? recorder = benchmarkStore is null ? null : new BenchmarkRecorder(benchmarkStore, benchmarkLog, host.Agent.LogDirectory);
if (benchmarkStore is not null)
    _ = Task.Run(async () =>
    {
        try
        {
            int closed = await benchmarkStore.CloseOrphansAsync(DateTimeOffset.UtcNow);
            if (closed > 0) benchmarkLog.Log(LogLevel.Information, ApiLogMessage.BenchmarkOrphansClosed, new { Count = closed }, tag: "API");
        }
        catch (Exception ex)
        {
            benchmarkLog.Log(LogLevel.Warning, ApiLogMessage.BenchmarkStoreUnreachable, exception: ex, tag: "API");
        }
    });
app.MapAdmin(benchmarks, new BenchmarkRunner(benchmarks, host.Engine.WorkspaceRoot, admin.BenchmarkApiUrl, "python3", admin.EvalRunner), admin.AdminKey, benchmarkStore, recorder,
    check => BenchmarkSettings.Of(check, host.Llm.Provider, host.Llm.Model, host.Agent.RolesEnabled, admin.EvalRunner), host.Engine.WorkspaceRoot);

app.MapSessions(host);

app.Run();
