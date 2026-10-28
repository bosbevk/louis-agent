using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Start;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Deletion;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Api.Messages;
using LouisAgent.Composition;
using LouisAgent.Core.Guardrails.Auth;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Search;
using Microsoft.Extensions.Logging;

// HTTP version of the ACP server, which also serves the web app: every API reply is JSON, and a message's answer
// streams as Server-Sent Events shaped like Claude's (message_start, content_block_start/delta/stop for thinking,
// text, tool_use and tool_result, message_stop).
// Start-up only: the routes are in Endpoints/, a turn's stream in Sessions/.

// Build the agent from the identities and packs the environment picks, and the web app around it.
var host = AgentHost.Build(Plugins.FromEnvironment());
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApi(host);
var app = builder.Build();

// The agent can run shell commands and write files, so its calls must present AGENT_API_KEY; which calls is
// AgentKeyGate's rule. Without a key the API refuses to start, here, before it listens, unless AGENT_AUTH=off says
// it is open on purpose (HostAuth is core's one rule for this and for the MCP server over HTTP).
string? apiKey = HostAuth.Key(Environment.GetEnvironmentVariable("AGENT_API_KEY"));
switch (HostAuth.AtStartup(HostAuth.Mode(Environment.GetEnvironmentVariable("AGENT_AUTH")), apiKey))
{
    case HostAuthDecision.Refuse:
        throw new InvalidOperationException(MessageText.Format(ErrorMessage.ApiKeyRequired,
            new { Setting = "AGENT_API_KEY" }));
    case HostAuthDecision.StartOpen:
        AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api").Log(LogLevel.Warning, ApiLogMessage.NoApiKey);
        break;
    case HostAuthDecision.Start:
        app.UseAgentKey(apiKey!); // apiKey is set whenever the decision is Start
        break;
}

// The Blazor web app (louis-agent.web): MapStaticAssets serves its files (including _framework/, compressed), and any
// path that isn't an API route loads it, e.g. /chat/{sessionId}.
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

// Every route of the agent's API is mapped by ApiMap, so the route tests build what runs; the admin portal's parts
// are built below and mapped on what it returns.
var api = app.MapApi(host);

// The admin portal, behind its own key (ADMIN_API_KEY, else AGENT_API_KEY; neither: closed); its settings are in
// AdminOptions. A run's script calls this API back at BENCHMARK_API_URL (the container's own port, by default).
// Each run is recorded into the store (EVALS_MONGO_URL), its cost joined from this instance's ledger; without a
// store no run starts. At start-up nothing runs, so a run still "running" was cut off by a restart.
var admin = AdminOptions.FromEnvironment();
var evalLog = AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api.Evals");

// The evals the runner lists, read on first use.
var evals = EvalList.From(EvalListing.ForRunner(admin.EvalRunner), evalLog);

// The stores for runs and for the people's grades share one Mongo; with no address there are none.
IEvalStore? evalStore = admin.EvalsMongoUrl is { } mongoUrl
    ? new MongoEvalStore(mongoUrl, admin.EvalsDatabase)
    : null;
IHumanReviewStore? reviewStore = admin.EvalsMongoUrl is { } reviewsUrl
    ? new MongoHumanReviewStore(reviewsUrl, admin.EvalsDatabase)
    : null;

// The judge checks sit beside the grades; a check's report is kept by its own recorder, never by the runs' store.
ICalibrationStore? calibrationStore = admin.EvalsMongoUrl is { } calibrationsUrl
    ? new MongoCalibrationStore(calibrationsUrl, admin.EvalsDatabase)
    : null;
CalibrationRecorder? calibrationRecorder =
    calibrationStore is null ? null : new CalibrationRecorder(calibrationStore, evalLog);

// A run's recorder writes it into the store, its cost read from this instance's ledger.
EvalRecorder? recorder = evalStore is null
    ? null
    : new EvalRecorder(evalStore, evalLog, host.Agent.LogDirectory);

// Delete what an older build stored (ADR 0049), then close the runs a restart cut off, in the background so start-up
// doesn't wait on Mongo; a store that doesn't answer is only a warning.
if (evalStore is not null)
    _ = Task.Run(async () =>
    {
        try
        {
            var deletion = new RunDeletion(evalStore, reviewStore, calibrationStore);
            await new OldRunsCleanup(evalStore, deletion, evalLog).RunAsync();

            int closed = await evalStore.CloseOrphansAsync(DateTimeOffset.UtcNow);
            if (closed > 0)
                evalLog.Log(LogLevel.Information, ApiLogMessage.BenchmarkOrphansClosed, new { Count = closed },
                    tag: "API");
        }
        catch (Exception ex)
        {
            evalLog.Log(LogLevel.Warning, ApiLogMessage.BenchmarkStoreUnreachable, exception: ex, tag: "API");
        }
    });

// The listing is read off the start-up path, so a warning lands in the start-up log (the first request would wait
// for it).
_ = Task.Run(evals.Warm);

// Map the admin routes: the runner, the stores and the settings each run reports.
api.MapAdmin(evals,
    new EvalProcess(evals, host.Engine.WorkspaceRoot, admin.BenchmarkApiUrl, evalRunner: admin.EvalRunner),
    admin.AdminKey, evalStore, recorder,
    check => EvalSettings.Of(check, host.Llm.Provider, host.Llm.Model, host.Agent.RolesEnabled, admin.EvalRunner,
        WebSearchProviderFactory.Resolve(host.Agent), PageRendererFactory.Resolve(host.Agent),
        WebSearchProviderFactory.IsReady(host.Agent), PageRendererFactory.IsReady(host.Agent)),
    host.Engine.WorkspaceRoot, reviewStore,
    calibrationStore, calibrationRecorder);

// Listen until the host is stopped.
app.Run();
