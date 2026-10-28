using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Api.Admin;
using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Reports.Trends;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Start;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using ManualAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Checks.ManualReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using TrendAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Trends.TrendReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using RunAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Runs.LiveRunRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using EventsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.ServerSentEventsResult<string>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// Does the work behind the admin portal's own routes: list the checks, show a check's manual and trend, and start
/// and follow a run. Each one stops early with its error and leaves the happy path for last.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object also owning the behaviour. The routes share the catalogue, the
/// runner, the store and the recorder, which <c>MapAdmin</c> is given as parameters and which aren't services. Holding
/// them here lets each route be a named method that <c>MapGet</c> points straight at, and a test call it with no HTTP.
/// Each method returns <c>TypedResults</c> with its answers declared (<c>ManualAnswer</c> and the rest, named once
/// above), Microsoft's preferred minimal-API style, so the build checks that a handler only answers what it says.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class AdminHandlers(
    EvalList catalog,
    EvalProcess runner,
    string? adminKey,
    IEvalStore? store,
    EvalRecorder? recorder,
    Func<EvalEntry, IReadOnlyDictionary<string, string>> settingsOf,
    string workspaceRoot)
{
    /// <summary>How long a run's "done" waits for its recording to finish.</summary>
    private static readonly TimeSpan RecordingWait = TimeSpan.FromSeconds(30);

    /// <summary>How many stored runs the runs page lists.</summary>
    private const int ListedRuns = 50;

    /// <summary>
    /// Lets a call through only with the admin key. If no key is set up you get a 403 that says what to set, and with
    /// the wrong key a 401.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), one per refusal, so the three outcomes read top to bottom.</remarks>
    public async ValueTask<object?> Gate(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var access = AdminGate.Check(adminKey, AdminGate.Presented(context.HttpContext.Request));
        if (access == AdminAccess.Closed)
            return ApiErrors.Forbidden(MessageText.Format(ApiUserText.AdminClosed));
        if (access == AdminAccess.Unauthorized)
            return ApiErrors.Unauthorized(MessageText.Format(ApiUserText.InvalidApiKey));
        return await next(context);
    }

    /// <summary>
    /// The page's first read: the checks, whether the store is there (for the page's banner), and the stored runs.
    /// </summary>
    public async Task<JsonHttpResult<BenchmarkListReply>> GetList(CancellationToken cancellationToken)
    {
        if (store is null) return ListOf([], null);
        try
        {
            return ListOf(await store.ListRunsAsync(limit: ListedRuns, cancellationToken: cancellationToken), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The page still loads: it shows the checks and says why the runs aren't there.
            return ListOf([], AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>
    /// Shows a check's manual, the markdown beside its script. The file is found through the catalogue, never through
    /// what the request says, so a request can't read any other file.
    /// </summary>
    public ManualAnswer GetManual(string check)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == check) is not { } known) return NoManual(check);
        if (EvalManuals.Read(workspaceRoot, known) is not { } markdown) return NoManual(check);
        return TypedResults.Json(ManualReply.Of(known, markdown), AdminJson.SnakeCase);
    }

    /// <summary>
    /// Shows how a check did over its finished runs: per mode, the explorer's nine rings as lines, and the same again
    /// per category.
    /// </summary>
    public async Task<TrendAnswer> GetTrend(string check, CancellationToken cancellationToken)
    {
        if (store is null) return NoStore();
        try
        {
            var trend = await EvalTrends.ComputeAsync(store, check, cancellationToken: cancellationToken);
            // The same trend per category, a chart of lines each, shaped like the overall trend's modes.
            var categories = CategoryTrendRow.Of(
                await CategoryTrends.ComputeAsync(store, check, cancellationToken: cancellationToken));
            return TypedResults.Json(TrendReply.Of(trend, categories), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>
    /// Starts a run from the page and records it as it goes. It won't start without a store, because a paid run whose
    /// results vanish is the worst outcome, and it won't start a second run while one is going.
    /// </summary>
    /// <remarks>Guard clauses (Fowler): each refusal stops early, the cheap ones before anything that costs.</remarks>
    public RunAnswer StartRun(string check, StartRunRequest? request)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == check) is not { } known)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.NoCheck,
                new
                {
                    Check = check,
                    Checks = string.Join(", ", catalog.Checks.Select(c => c.Id))
                }));

        // What to do with the web, checked before anything that costs: a request no run can do is a 400 on its own.
        var (options, refusal) = EvalRunOptions.Parse(request?.Web, request?.Replay);
        if (refusal is not null) return ApiErrors.BadRequest(refusal);
        if (recorder is null) return NoStore();
        if (runner.IsRunning) return Running();
        // Another start can win the race between the check above and this one, so a refused start is a 409 too.
        if (runner.Start(check, request?.Repeat ?? 1, options) is not { } run) return Running();

        // Record it with the settings it ran under, the web options included.
        recorder.Attach(run, EvalSettings.WithOptions(settingsOf(known), run.Options));
        return TypedResults.Json(LiveRunRow.Of(run), AdminJson.SnakeCase, statusCode: StatusCodes.Status202Accepted);
    }

    /// <summary>Hands back the run that's going, or the last one, so a reloaded page can pick it up again.</summary>
    public RunAnswer GetCurrent()
    {
        if (runner.Current is not { } run) return NoRun();
        return TypedResults.Json(LiveRunRow.Of(run), AdminJson.SnakeCase);
    }

    /// <summary>Streams the current run's lines to the page, then a last "done" saying whether it was stored.</summary>
    public EventsAnswer GetEvents(string id, CancellationToken cancellationToken)
    {
        if (runner.Current is not { } run || run.Id != id) return NoRun();
        return TypedResults.ServerSentEvents(Events(run, cancellationToken));
    }

    /// <summary>Builds the page's list from these runs, with whether the store is there and why not.</summary>
    private JsonHttpResult<BenchmarkListReply> ListOf(IReadOnlyList<EvalDocuments.Run> runs, string? error) =>
        TypedResults.Json(BenchmarkListReply.Of(catalog, workspaceRoot, StoreRow.Of(store is not null, error),
            [.. runs.Select(StoredRunRow.Of)]), AdminJson.SnakeCase);

    /// <summary>The 404 for a check we have no manual for.</summary>
    private static JsonHttpResult<ApiError> NoManual(string check) =>
        ApiErrors.NotFound(MessageText.Format(ApiUserText.NoManual, new { Check = check }));

    /// <summary>The 503 for a call that needs the store when there is none.</summary>
    private static JsonHttpResult<ApiError> NoStore() =>
        ApiErrors.Unavailable(MessageText.Format(ApiUserText.NoBenchmarkStore));

    /// <summary>The 404 for a run that isn't the current one.</summary>
    private static JsonHttpResult<ApiError> NoRun() =>
        ApiErrors.NotFound(MessageText.Format(ApiUserText.NoBenchmarkRun));

    /// <summary>The 409 for a start while another run is still going.</summary>
    private JsonHttpResult<ApiError> Running() =>
        ApiErrors.Conflict(MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));

    /// <summary>
    /// Streams a run's lines to the page as they come, then one last "done" with whether the recording kept
    /// everything.
    /// </summary>
    private async IAsyncEnumerable<SseItem<string>> Events(RunningEval run,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (string line in runner.EventsAsync(run.Id, cancellationToken))
            yield return new SseItem<string>(line, "line");

        // The lines have ended: wait for the recording, then send the run once more with how it was stored.
        var stored = await StoredAsync(run.Id, cancellationToken);
        yield return new SseItem<string>(JsonSerializer.Serialize(LiveRunRow.Of(run, stored), AdminJson.SnakeCase),
            "done");
    }

    /// <summary>
    /// Waits, up to a limit, for the run's recording to finish and says whether it kept everything. It says no if the
    /// recording failed, took too long or never started.
    /// </summary>
    private async Task<EvalRecorder.Result> StoredAsync(string id, CancellationToken cancellationToken)
    {
        if (recorder?.RecordingOf(id) is not { } recording) return new EvalRecorder.Result(false, null);

        // A recording that takes longer than the wait counts as not stored; the page shouldn't hang on it.
        try
        {
            return await recording.WaitAsync(RecordingWait, cancellationToken);
        }
        catch (TimeoutException)
        {
            return new EvalRecorder.Result(false, null);
        }
    }
}
