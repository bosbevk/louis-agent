using LouisAgent.Api.Config;
using LouisAgent.Api.Endpoints.Admin;
using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Config;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Endpoints.Runs;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Messages;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Start;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Deletion;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Composition.Logging;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;

namespace LouisAgent.Api.Evals;

/// <summary>
/// The eval studio as one block your program calls after <c>MapLouisAgentApi</c> (CL57): the runner, the stores for
/// runs, grades, drafts and judge checks, the recorder, and every admin route for them, behind the same admin key as
/// the API's own admin routes. A program that doesn't call it ships no studio and no MongoDB driver.
/// </summary>
/// <remarks>
/// Extension Method, as <c>ApiStartup</c> is put together, so a program reads as the app it is; it refuses to map
/// until the API's key check has run, the same fail-closed rule.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0092-a-project-holds-what-is-reused-together.md">ADR 0092: a project holds what is reused together</seealso>
public static class EvalsStartup
{
    /// <summary>
    /// Every admin route of the studio, and what it starts in the background. Its settings are in
    /// <see cref="EvalOptions"/>. A run's script calls this API back at BENCHMARK_API_URL (the container's own port,
    /// by default). Each run is recorded into the store (EVALS_MONGO_URL), its cost joined from this instance's
    /// ledger; without a store no run starts. At start-up nothing runs, so a run still "running" was cut off by a
    /// restart.
    /// </summary>
    public static WebApplication MapLouisAgentEvals(this WebApplication app, AgentHostContext host)
    {
        ApiStartup.RequireKeyChecked(app);

        var evals = EvalOptions.FromEnvironment();
        var evalLog = AgentLogging.LoggerFactory.CreateLogger("LouisAgent.Api.Evals");

        // The evals the runner lists, read on first use.
        var list = EvalList.From(EvalListing.ForRunner(evals.EvalRunner), evalLog);

        // The stores for runs and for the people's grades share one Mongo; with no address there are none.
        IEvalStore? evalStore = evals.EvalsMongoUrl is { } mongoUrl
            ? new MongoEvalStore(mongoUrl, evals.EvalsDatabase)
            : null;
        IHumanReviewStore? reviewStore = evals.EvalsMongoUrl is { } reviewsUrl
            ? new MongoHumanReviewStore(reviewsUrl, evals.EvalsDatabase)
            : null;

        // The drafts made from stored runs wait beside the grades until F49.5 publishes them; without Mongo they live
        // in memory.
        IEvalDraftStore draftStore = evals.EvalsMongoUrl is { } draftsUrl
            ? new MongoEvalDraftStore(draftsUrl, evals.EvalsDatabase)
            : new InMemoryEvalDraftStore();

        // The judge checks sit beside the grades; a check's report is kept by its own recorder, never by the runs'
        // store.
        ICalibrationStore? calibrationStore = evals.EvalsMongoUrl is { } calibrationsUrl
            ? new MongoCalibrationStore(calibrationsUrl, evals.EvalsDatabase)
            : null;
        CalibrationRecorder? calibrationRecorder =
            calibrationStore is null ? null : new CalibrationRecorder(calibrationStore, evalLog);

        // A run's recorder writes it into the store, its cost read from this instance's ledger.
        EvalRecorder? recorder = evalStore is null
            ? null
            : new EvalRecorder(evalStore, evalLog, host.Agent.LogDirectory);

        // Delete what an older build stored (ADR 0049), then close the runs a restart cut off, in the background so
        // start-up doesn't wait on Mongo; a store that doesn't answer is only a warning.
        if (evalStore is not null)
            _ = Task.Run(() => TidyAsync(evalStore, reviewStore, calibrationStore, evalLog));

        // The listing is read off the start-up path, so a warning lands in the start-up log (the first request would
        // wait for it).
        _ = Task.Run(list.Warm);

        // The studio's routes on the admin group, behind the key: the runner, the stores and the settings each run
        // reports, which name the web the engine has (CL57).
        app.MapAdminGroup(AdminOptions.FromEnvironment().AdminKey).MapEvalAdmin(list,
            new EvalProcess(list, host.Engine.WorkspaceRoot, evals.BenchmarkApiUrl, evalRunner: evals.EvalRunner),
            evalStore, recorder,
            check => EvalSettings.Of(check, host.Llm.Provider, host.Llm.Model, host.Agent.RolesEnabled,
                evals.EvalRunner, host.Engines.Context.Web.Setup),
            host.Engine.WorkspaceRoot, reviewStore,
            calibrationStore, calibrationRecorder, drafts: draftStore,
            studio: StudioRunner.ForRunner(evals.EvalRunner));

        return app;
    }

    /// <summary>Deletes what an older build stored and closes the runs a restart cut off; a store that doesn't answer is a warning.</summary>
    private static async Task TidyAsync(IEvalStore evalStore, IHumanReviewStore? reviewStore,
        ICalibrationStore? calibrationStore, ILogger evalLog)
    {
        try
        {
            var deletion = new RunDeletion(evalStore, reviewStore, calibrationStore);
            await new OldRunsCleanup(evalStore, deletion, evalLog).RunAsync();
            int closed = await evalStore.CloseOrphansAsync(TimeProvider.System.GetUtcNow());
            if (closed > 0)
                evalLog.Log(LogLevel.Information, EvalApiLogMessage.BenchmarkOrphansClosed, new { Count = closed },
                    tag: "API");
        }
        catch (Exception ex)
        {
            evalLog.Log(LogLevel.Warning, EvalApiLogMessage.BenchmarkStoreUnreachable, exception: ex, tag: "API");
        }
    }
}
