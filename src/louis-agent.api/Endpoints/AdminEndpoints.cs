using System.Net.ServerSentEvents;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Services;
using LouisAgent.Api.Services.Benchmarks;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The admin portal's endpoints (F27), all under <c>/admin</c> and all behind <see cref="AdminGate"/>: with no admin
/// key configured every call is 403 and says what to set; with one, a call must present it. The checks and a run of
/// one, recorded into the benchmark store as it runs (F31); without a store no run starts.
/// </summary>
internal static class AdminEndpoints
{
    /// <summary>How long a run's "done" waits for its recording to finish (the ledger join and the last writes).</summary>
    private static readonly TimeSpan RecordingWait = TimeSpan.FromSeconds(30);

    public static void MapAdmin(this WebApplication app, BenchmarkCatalog catalog, BenchmarkRunner runner, string? adminKey,
        BenchmarkRecorder? recorder, Func<BenchmarkCheck, IReadOnlyDictionary<string, string>> settingsOf)
    {
        var admin = app.MapGroup("/admin").AddEndpointFilter(async (context, next) =>
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

        admin.MapGet("/benchmarks", () => Results.Json(new
        {
            checks = catalog.Checks.Select(c => new { id = c.Id, title = c.Title, script = c.Script }),
        }));

        // A run from the page. 202 with the run, recorded as it goes; 503 without a store (a paid run whose results vanish
        // is the worst outcome); 409 while one runs; its lines as SSE, then "done" with whether it was stored.
        admin.MapPost("/benchmarks/{check}/runs", (string check, StartRunRequest? request) =>
        {
            if (catalog.Checks.FirstOrDefault(c => c.Id == check) is not { } known)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
                    MessageText.Format(ApiUserText.NoCheck, new { Check = check, Checks = string.Join(", ", catalog.Checks.Select(c => c.Id)) }));
            if (recorder is null)
                return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", MessageText.Format(ApiUserText.NoBenchmarkStore));
            if (runner.IsRunning)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
            if (runner.Start(check, request?.Repeat ?? 1) is not { } run)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
            recorder.Attach(run, settingsOf(known));
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

    private static object Row(BenchmarkRun run, BenchmarkRecorder.Result? stored = null) => new
    {
        id = run.Id, check = run.Check, repeat = run.Repeat, status = run.Status.ToString().ToLowerInvariant(),
        exit_code = run.ExitCode, started_at = run.StartedAt, ended_at = run.EndedAt, lines = run.Lines.Count,
        stored = stored?.Stored, store_error = stored?.StoreError,
    };
}

/// <summary>The body of <c>POST /admin/benchmarks/{check}/runs</c>: how many repeats (1 to the runner's cap).</summary>
internal sealed record StartRunRequest(int? Repeat);
