using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Admin.AdminRows;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The admin portal's own routes as named methods: the checks, a check's manual and trend, and starting and following
/// a run. Each one stops early with its error and leaves the happy path for last.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object also owning the behaviour. The routes share the catalogue, the
/// runner, the store and the recorder, which `MapAdmin` is given as parameters and which aren't services. Holding them
/// here lets each route be a named method that `MapGet` points straight at, where a lambda would have to close over
/// them. Without it the routes stay lambdas, or every one takes seven arguments.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed class AdminHandlers(
    BenchmarkCatalog catalog,
    BenchmarkRunner runner,
    string? adminKey,
    IBenchmarkStore? store,
    BenchmarkRecorder? recorder,
    Func<BenchmarkCheck, IReadOnlyDictionary<string, string>> settingsOf,
    string workspaceRoot)
{
    /// <summary>How long a run's "done" waits for its recording to finish.</summary>
    private static readonly TimeSpan RecordingWait = TimeSpan.FromSeconds(30);

    /// <summary>How many runs the Benchmarks page lists.</summary>
    private const int ListedRuns = 50;

    /// <summary>
    /// Lets a call through only with the admin key: a 403 that says what to set when none is configured, a 401 when
    /// it's wrong.
    /// </summary>
    public async ValueTask<object?> Gate(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var access = AdminGate.Check(adminKey, AdminGate.Presented(context.HttpContext.Request));
        if (access == AdminAccess.Closed)
            return ApiErrors.Result(StatusCodes.Status403Forbidden, "permission_error",
                MessageText.Format(ApiUserText.AdminClosed));
        if (access == AdminAccess.Unauthorized)
            return ApiErrors.Result(StatusCodes.Status401Unauthorized, "authentication_error",
                MessageText.Format(ApiUserText.InvalidApiKey));
        return await next(context);
    }

    /// <summary>
    /// The page's first read: the checks, whether the store is there (the page's banner), and the stored runs.
    /// </summary>
    public async Task<IResult> GetList(CancellationToken cancellationToken)
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
    /// A check's manual: the markdown beside its script, found from the catalogue, never from the request.
    /// </summary>
    public IResult GetManual(string check)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == check) is not { } known) return NoManual(check);
        if (BenchmarkManuals.Read(workspaceRoot, known) is not { } markdown) return NoManual(check);
        return Results.Json(ManualReply.Of(known, markdown), AdminJson.SnakeCase);
    }

    /// <summary>
    /// A check's trend over its finished runs: per mode, the explorer's nine rings as lines over the runs, and the same
    /// per category.
    /// </summary>
    public Task<IResult> GetTrend(string check, CancellationToken cancellationToken) =>
        AdminEndpoints.FromStore(store, async s =>
        {
            var trend = await BenchmarkTrends.ComputeAsync(s, check, cancellationToken: cancellationToken);
            // F18 step 5c.4: the same trend per category, a chart of lines each, in the shape the overall trend's modes
            // are.
            var categories = CategoryTrendRow.Of(
                await CategoryTrends.ComputeAsync(s, check, cancellationToken: cancellationToken));
            return Results.Json(TrendReply.Of(trend, categories), AdminJson.SnakeCase);
        });

    /// <summary>
    /// Starts a run from the page: 202 with the run, recorded as it goes; 503 without a store (a paid run whose results
    /// vanish is the worst outcome); 409 while one runs.
    /// </summary>
    public IResult StartRun(string check, StartRunRequest? request)
    {
        if (catalog.Checks.FirstOrDefault(c => c.Id == check) is not { } known)
            return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
                MessageText.Format(ApiUserText.NoCheck,
                    new
                    {
                        Check = check,
                        Checks = string.Join(", ", catalog.Checks.Select(c => c.Id))
                    }));

        // The web (F18 step 5.5), checked before anything that costs: a request that can't be what a run does is a 400
        // on its own.
        var (options, refusal) = BenchmarkRunOptions.Parse(request?.Web, request?.Replay);
        if (refusal is not null)
            return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", refusal);
        if (recorder is null)
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
                MessageText.Format(ApiUserText.NoBenchmarkStore));
        if (runner.IsRunning) return Running();
        if (runner.Start(check, request?.Repeat ?? 1, options) is not { } run) return Running();

        recorder.Attach(run, BenchmarkSettings.WithOptions(settingsOf(known), run.Options));
        return Results.Json(Row(run), statusCode: StatusCodes.Status202Accepted);
    }

    /// <summary>The run in progress, or the last one, so a reloaded page can pick it up again.</summary>
    public IResult GetCurrent()
    {
        if (runner.Current is not { } run) return NoRun();
        return Results.Json(Row(run));
    }

    /// <summary>A run's lines as server-sent events, then "done" with whether it was stored.</summary>
    public IResult GetEvents(string id, CancellationToken cancellationToken)
    {
        if (runner.Current is not { } run || run.Id != id) return NoRun();
        return TypedResults.ServerSentEvents(Events(run, cancellationToken));
    }

    /// <summary>The list reply for these runs, with the store's state beside them.</summary>
    private IResult ListOf(IReadOnlyList<BenchmarkDocuments.Run> runs, string? error) =>
        Results.Json(BenchmarkListReply.Of(catalog, workspaceRoot, StoreRow.Of(store is not null, error),
            [.. runs.Select(RunRow)]), AdminJson.SnakeCase);

    /// <summary>The 404 for a check with no manual.</summary>
    private static IResult NoManual(string check) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
            MessageText.Format(ApiUserText.NoManual, new { Check = check }));

    /// <summary>The 404 for a run that isn't the current one.</summary>
    private static IResult NoRun() =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
            MessageText.Format(ApiUserText.NoBenchmarkRun));

    /// <summary>The 409 for a start while another run is still going.</summary>
    private IResult Running() =>
        ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error",
            MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));

    /// <summary>
    /// Streams a run's lines to the page as they come, then one last "done" with whether the recording kept
    /// everything.
    /// </summary>
    private async IAsyncEnumerable<SseItem<string>> Events(BenchmarkRun run,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (string line in runner.EventsAsync(run.Id, cancellationToken))
            yield return new SseItem<string>(line, "line");
        var stored = await StoredAsync(run.Id, cancellationToken);
        yield return new SseItem<string>(JsonSerializer.Serialize(Row(run, stored)), "done");
    }

    /// <summary>
    /// Waits, up to a limit, for the run's recording to finish and says whether it kept everything. It says no if the
    /// recording failed, took too long or never started.
    /// </summary>
    private async Task<BenchmarkRecorder.Result> StoredAsync(string id, CancellationToken cancellationToken)
    {
        if (recorder?.RecordingOf(id) is not { } recording) return new BenchmarkRecorder.Result(false, null);
        try
        {
            return await recording.WaitAsync(RecordingWait, cancellationToken);
        }
        catch (TimeoutException)
        {
            return new BenchmarkRecorder.Result(false, null);
        }
    }
}
