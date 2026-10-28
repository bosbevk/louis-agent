using LouisAgent.Api.Endpoints.Admin.Evals;
using LouisAgent.Api.Endpoints.Admin.Reviews;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// Hangs every admin portal route on the API: the checks, starting a run, and reading back what it recorded. All of
/// them sit behind the admin key, and without a store no run starts.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class AdminEndpoints
{
    /// <summary>
    /// Adds the admin routes under <c>/admin</c>, behind the key. The clock stamps the notes, so a test can fix it.
    /// </summary>
    public static void MapAdmin(this IEndpointRouteBuilder routes, EvalList catalog, EvalProcess runner,
        string? adminKey,
        IEvalStore? store, EvalRecorder? recorder,
        Func<EvalEntry, IReadOnlyDictionary<string, string>> settingsOf,
        string workspaceRoot, IHumanReviewStore? reviews = null, ICalibrationStore? calibrations = null,
        CalibrationRecorder? calibrationRecorder = null,
        TimeProvider? time = null)
    {
        var handlers = new AdminHandlers(catalog, runner, adminKey, store, recorder, settingsOf, workspaceRoot);
        // One group for all of them, so the key check runs before any route's own work.
        var admin = routes.MapGroup("/admin").WithAccess(RouteAccess.AdminKey).AddEndpointFilter(handlers.Gate);

        admin.MapGet("/benchmarks", handlers.GetList);
        admin.MapGet("/benchmarks/{check}/manual", handlers.GetManual);
        admin.MapGet("/benchmarks/checks/{check}/trend", handlers.GetTrend);
        admin.MapEvals(catalog, store, workspaceRoot);

        admin.MapStoredRuns(store, runner, reviews, calibrations);
        admin.MapReviews(store, reviews);
        admin.MapNotes(store, time ?? TimeProvider.System);
        admin.MapRescore(catalog, runner, store, recorder, settingsOf);
        admin.MapCalibrations(catalog, runner, store, reviews, calibrations, calibrationRecorder);

        admin.MapPost("/benchmarks/{check}/runs", handlers.StartRun);
        admin.MapGet("/benchmarks/runs/current", handlers.GetCurrent);
        admin.MapGet("/benchmarks/runs/{id}/events", handlers.GetEvents);
    }

    /// <summary>
    /// Reads from the store for a route. If there's no store, or it doesn't answer, you get a 503 that says why.
    /// </summary>
    internal static async Task<IResult> FromStore(IEvalStore? store, Func<IEvalStore, Task<IResult>> read)
    {
        if (store is null)
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
                MessageText.Format(ApiUserText.NoBenchmarkStore));

        // Any failure but a cancel is the store not answering: the caller gets its reason, not a 500.
        try
        {
            return await read(store);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", Unavailable(ex));
        }
    }

    /// <summary>What we tell the person when the store didn't answer, with its reason.</summary>
    internal static string Unavailable(Exception ex) =>
        MessageText.Format(ApiUserText.BenchmarkStoreUnavailable, new { Reason = ex.Message });
}

/// <summary>
/// What the page sends to start a run: how many repeats, and for an eval what it does with the web. It can record the
/// web (or turn it off), or replay an earlier run's answers, but not both.
/// </summary>
internal sealed record StartRunRequest(int? Repeat, string? Web = null, string? Replay = null);
