using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// Hangs the routes that read a stored run, and delete it: the run with everything it kept, one turn's stream events,
/// and the run as the protocol's own lines. They sit in the admin group, so the key covers them; the work is in
/// <c>StoredRunHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class StoredRunEndpoints
{
    /// <summary>Adds the reads and the delete of a stored run.</summary>
    public static void MapStoredRuns(this RouteGroupBuilder admin, IBenchmarkStore? store, BenchmarkRunner runner,
        IHumanReviewStore? reviews = null,
        ICalibrationStore? calibrations = null)
    {
        var handlers = new StoredRunHandlers(store, runner, reviews, calibrations);

        admin.MapGet("/benchmarks/runs/{id}", handlers.GetRun);
        admin.MapGet("/benchmarks/runs/{id}/turns/{mode}/{repeat:int}/{task}/{turn:int}/events",
            handlers.GetTurnEvents);
        admin.MapGet("/benchmarks/runs/{id}/protocol", handlers.GetProtocol);
        admin.MapDelete("/benchmarks/runs/{id}", handlers.DeleteRun);
    }
}
