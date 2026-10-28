using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Endpoints.Admin.Evals;
using LouisAgent.Api.Endpoints.Admin.Reviews;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// The admin portal's routes: the checks, starting a run, and reading what it recorded. Everything is behind the admin
/// key: with none configured every call is a 403 that says what to set, with one a call has to present it. Without a
/// store no run starts.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class AdminEndpoints
{
    /// <summary>
    /// Adds the admin portal's routes, all behind the admin key. The clock is for the notes' times, and a test can fix
    /// it.
    /// </summary>
    public static void MapAdmin(this IEndpointRouteBuilder routes, BenchmarkCatalog catalog, BenchmarkRunner runner,
        string? adminKey,
        IBenchmarkStore? store, BenchmarkRecorder? recorder,
        Func<BenchmarkCheck, IReadOnlyDictionary<string, string>> settingsOf,
        string workspaceRoot, IHumanReviewStore? reviews = null, ICalibrationStore? calibrations = null,
        CalibrationRecorder? calibrationRecorder = null,
        TimeProvider? time = null)
    {
        var handlers = new AdminHandlers(catalog, runner, adminKey, store, recorder, settingsOf, workspaceRoot);
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

    /// <summary>Runs a read of the store: a 503 without a store, or when it doesn't answer, saying why.</summary>
    internal static async Task<IResult> FromStore(IBenchmarkStore? store, Func<IBenchmarkStore, Task<IResult>> read)
    {
        if (store is null)
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
                MessageText.Format(ApiUserText.NoBenchmarkStore));
        try
        {
            return await read(store);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", Unavailable(ex));
        }
    }

    /// <summary>The words for a store that didn't answer.</summary>
    internal static string Unavailable(Exception ex) =>
        MessageText.Format(ApiUserText.BenchmarkStoreUnavailable, new { Reason = ex.Message });
}

/// <summary>
/// What starts a run: how many repeats, and for an eval how it handles the web. Either record it (or turn it off), or
/// replay an earlier run's answers, not both.
/// </summary>
internal sealed record StartRunRequest(int? Repeat, string? Web = null, string? Replay = null);
