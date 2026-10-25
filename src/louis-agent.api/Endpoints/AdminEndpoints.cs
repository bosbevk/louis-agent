using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Endpoints.Routing;
using System.Net.ServerSentEvents;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Admin.AdminRows;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The admin portal's endpoints (F27), all under <c>/admin</c> and all behind <see cref="AdminGate"/>: with no admin
/// key configured every call is 403 and says what to set; with one, a call must present it. The checks and a run of
/// one, recorded into the benchmark store as it runs (F31); without a store no run starts. The rows they send are
/// <see cref="AdminRows"/>.
/// </summary>
internal static class AdminEndpoints
{
    /// <summary>How long a run's "done" waits for its recording to finish (the ledger join and the last writes).</summary>
    private static readonly TimeSpan RecordingWait = TimeSpan.FromSeconds(30);

    /// <summary>How many runs the Benchmarks page lists.</summary>
    private const int ListedRuns = 50;

    public static void MapAdmin(this IEndpointRouteBuilder routes, BenchmarkCatalog catalog, BenchmarkRunner runner, string? adminKey,
        IBenchmarkStore? store, BenchmarkRecorder? recorder, Func<BenchmarkCheck, IReadOnlyDictionary<string, string>> settingsOf,
        string workspaceRoot)
    {
        var admin = routes.MapGroup("/admin").WithAccess(RouteAccess.AdminKey).AddEndpointFilter(async (context, next) =>
        {
            switch (AdminGate.Check(adminKey, AdminGate.Presented(context.HttpContext.Request)))
            {
                case AdminAccess.Closed:
                    return ApiErrors.Result(StatusCodes.Status403Forbidden, "permission_error", MessageText.Format(ApiUserText.AdminClosed));
                case AdminAccess.Unauthorized:
                    return ApiErrors.Result(StatusCodes.Status401Unauthorized, "authentication_error", MessageText.Format(ApiUserText.InvalidApiKey));
                default:
                    return await next(context);
            }
        });

        // The page's first read (F31): the checks, whether the store is there (the page's banner), and the stored runs.
        admin.MapGet("/benchmarks", async (CancellationToken cancellationToken) =>
        {
            IReadOnlyList<BenchmarkDocuments.Run> runs = [];
            string? error = null;
            if (store is not null)
                try { runs = await store.ListRunsAsync(limit: ListedRuns, cancellationToken: cancellationToken); }
                catch (Exception ex) when (ex is not OperationCanceledException) { error = Unavailable(ex); }
            return Results.Json(new
            {
                checks = catalog.Checks.Select(c => new { id = c.Id, title = c.Title, script = c.Script, manual = BenchmarkManuals.Has(workspaceRoot, c) }),
                store = new { configured = store is not null, error },
                runs = runs.Select(RunRow),
            });
        });

        // A check's manual (step 10b): the markdown beside its script, found from the catalogue, never from the request.
        admin.MapGet("/benchmarks/{check}/manual", (string check) =>
            catalog.Checks.FirstOrDefault(c => c.Id == check) is { } known && BenchmarkManuals.Read(workspaceRoot, known) is { } markdown
                ? Results.Json(new { check = known.Id, title = known.Title, markdown })
                : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoManual, new { Check = check })));

        // A check's trend over its finished runs (step 11a): per mode, the explorer's nine rings as lines over the runs.
        admin.MapGet("/benchmarks/checks/{check}/trend", (string check, CancellationToken cancellationToken) => FromStore(store, async s =>
        {
            var trend = await BenchmarkTrends.ComputeAsync(s, check, cancellationToken: cancellationToken);
            return Results.Json(new
            {
                check = trend.Check,
                modes = trend.Modes.Select(m => new
                {
                    name = m.Name,
                    points = m.Points.Select(p => new
                    {
                        run = p.Run, at = p.At, cost = p.Cost, score = p.Score, conversations = p.Conversations,
                        tokens = p.Tokens, seconds_per_turn = p.SecondsPerTurn, cache_share = p.CacheShare, sourced = p.Sourced,
                        tools_failed = p.ToolsFailed, cost_per_answer = p.CostPerAnswer, sites = p.Sites,
                        pass_at_k = p.PassAtK, pass_hat_k = p.PassHatK, k = p.K,
                    }),
                }),
            });
        }));

        admin.MapStoredRuns(store, runner);

        // A run from the page. 202 with the run, recorded as it goes; 503 without a store (a paid run whose results vanish
        // is the worst outcome); 409 while one runs; its lines as SSE, then "done" with whether it was stored.
        admin.MapPost("/benchmarks/{check}/runs", (string check, StartRunRequest? request) =>
        {
            if (catalog.Checks.FirstOrDefault(c => c.Id == check) is not { } known)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
                    MessageText.Format(ApiUserText.NoCheck, new { Check = check, Checks = string.Join(", ", catalog.Checks.Select(c => c.Id)) }));
            // The web (F18 step 5.5), checked before anything that costs: a request that can't be what a run does is a 400 on its own.
            var (options, refusal) = BenchmarkRunOptions.Parse(request?.Web, request?.Replay, known);
            if (refusal is not null)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", refusal);
            if (recorder is null)
                return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", MessageText.Format(ApiUserText.NoBenchmarkStore));
            if (runner.IsRunning)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
            if (runner.Start(check, request?.Repeat ?? 1, options) is not { } run)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
            recorder.Attach(run, BenchmarkSettings.WithOptions(settingsOf(known), run.Options));
            return Results.Json(Row(run), statusCode: StatusCodes.Status202Accepted);
        });

        // The run in progress, or the last one, so a reloaded page can pick it up again.
        admin.MapGet("/benchmarks/runs/current", () => runner.Current is { } run
            ? Results.Json(Row(run))
            : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBenchmarkRun)));

        admin.MapGet("/benchmarks/runs/{id}/events", (string id, CancellationToken cancellationToken) =>
            runner.Current is { } run && run.Id == id
                ? (IResult)TypedResults.ServerSentEvents(Events(runner, recorder, run, cancellationToken))
                : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBenchmarkRun)));
    }

    private static async IAsyncEnumerable<SseItem<string>> Events(BenchmarkRunner runner, BenchmarkRecorder? recorder, BenchmarkRun run,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (string line in runner.EventsAsync(run.Id, cancellationToken))
            yield return new SseItem<string>(line, "line");
        var stored = await StoredAsync(recorder, run.Id, cancellationToken);
        yield return new SseItem<string>(System.Text.Json.JsonSerializer.Serialize(Row(run, stored)), "done");
    }

    /// <summary>A read of the store: 503 without one, or when it doesn't answer, saying why.</summary>
    internal static async Task<IResult> FromStore(IBenchmarkStore? store, Func<IBenchmarkStore, Task<IResult>> read)
    {
        if (store is null)
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", MessageText.Format(ApiUserText.NoBenchmarkStore));
        try
        {
            return await read(store);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", Unavailable(ex));
        }
    }

    internal static string Unavailable(Exception ex) => MessageText.Format(ApiUserText.BenchmarkStoreUnavailable, new { Reason = ex.Message });

    /// <summary>Whether the run's recording kept everything; false when it failed, took too long or never started.</summary>
    private static async Task<BenchmarkRecorder.Result> StoredAsync(BenchmarkRecorder? recorder, string id, CancellationToken cancellationToken)
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

/// <summary>
/// The body of <c>POST /v1/admin/benchmarks/{check}/runs</c>: how many repeats (1 to the runner's cap), and for an eval how it handles the web
/// (F18 step 5.5): <c>web</c> (<c>record</c> or <c>off</c>) or <c>replay</c> (a recorded run's id, with the model's answers), not both.
/// </summary>
internal sealed record StartRunRequest(int? Repeat, string? Web = null, string? Replay = null);
