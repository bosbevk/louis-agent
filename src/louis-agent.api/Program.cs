using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Composition;
using LouisAgent.Core.Guardrails.Auth;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

// HTTP version of the ACP server, which also serves the web app: every API reply is JSON, and a message's answer streams as Server-Sent Events shaped
// like Claude's (message_start, content_block_start/delta/stop for thinking, text, tool_use and tool_result, message_stop).
// Start-up only: the routes are in Endpoints/, a turn's stream in Sessions/.
var host = AgentHost.Build(Plugins.FromEnvironment());

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApi(host);
var app = builder.Build();

// The agent can run shell commands and write files, so its calls must present AGENT_API_KEY; which calls is AgentKeyGate's
// rule. FX18: without one the API refuses to start, here, before it listens, unless AGENT_AUTH=off says it is
// open on purpose (HostAuth, core's one rule for both this and the MCP server over HTTP).
string? apiKey = HostAuth.Key(Environment.GetEnvironmentVariable("AGENT_API_KEY"));
switch (HostAuth.AtStartup(HostAuth.Mode(Environment.GetEnvironmentVariable("AGENT_AUTH")), apiKey))
{
    case HostAuthDecision.Refuse:
        throw new InvalidOperationException(MessageText.Format(ErrorMessage.ApiKeyRequired, new { Setting = "AGENT_API_KEY" }));
    case HostAuthDecision.StartOpen:
        AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api").Log(LogLevel.Warning, ApiLogMessage.NoApiKey);
        break;
    case HostAuthDecision.Start:
        app.UseAgentKey(apiKey!);   // apiKey is set whenever the decision is Start
        break;
}

// The Blazor web app (louis-agent.web): MapStaticAssets serves its files (including _framework/, compressed), and any
// path that isn't an API route loads it, e.g. /chat/{sessionId}.
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

// Every route of the agent's API is mapped by ApiMap (CL20), so the route tests build what runs; the admin portal's parts, which
// F18 changes, are built below and mapped on what it returns.
var api = app.MapApi(host);

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
// The listing is read off the start-up path, so a warning lands in the start-up log (the first request would wait for it).
_ = Task.Run(benchmarks.Warm);
api.MapAdmin(benchmarks, new BenchmarkRunner(benchmarks, host.Engine.WorkspaceRoot, admin.BenchmarkApiUrl, evalRunner: admin.EvalRunner), admin.AdminKey, benchmarkStore, recorder,
    check => BenchmarkSettings.Of(check, host.Llm.Provider, host.Llm.Model, host.Agent.RolesEnabled, admin.EvalRunner), host.Engine.WorkspaceRoot);

app.Run();
