using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The judge checks of a stored run (F18 step 6a.5b, ADR 0039: a resource, <c>calibrations</c>, under the run): start
/// one, and list the reports kept. They belong to the same <c>/admin</c> group as the rest, so the key gate covers
/// them; the work is <c>CalibrationHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class CalibrationEndpoints
{
    /// <summary>Adds the judge-check routes to the admin group.</summary>
    public static void MapCalibrations(this RouteGroupBuilder admin, BenchmarkCatalog catalog, BenchmarkRunner runner,
        IBenchmarkStore? store, IHumanReviewStore? reviews,
        ICalibrationStore? calibrations, CalibrationRecorder? recorder)
    {
        var handlers = new CalibrationHandlers(catalog, runner, store, reviews, calibrations, recorder);

        admin.MapPost("/benchmarks/runs/{id}/calibrations/{grader}", handlers.StartCalibration);
        admin.MapGet("/benchmarks/runs/{id}/calibrations", handlers.GetCalibrations);
    }
}
