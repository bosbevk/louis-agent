using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Endpoints.Admin.Reviews;

/// <summary>
/// Hangs the routes for a stored run's judge checks, a <c>calibrations</c> resource under the run (ADR 0039): start
/// one, and list the reports kept. They sit in the admin group, so the key covers them; the work is in
/// <c>CalibrationHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class CalibrationEndpoints
{
    /// <summary>Adds the judge-check routes to the admin group.</summary>
    public static void MapCalibrations(this RouteGroupBuilder admin, EvalList catalog, EvalProcess runner,
        IEvalStore? store, IHumanReviewStore? reviews,
        ICalibrationStore? calibrations, CalibrationRecorder? recorder)
    {
        var handlers = new CalibrationHandlers(catalog, runner, store, reviews, calibrations, recorder);

        admin.MapPost("/benchmarks/runs/{id}/calibrations/{grader}", handlers.StartCalibration);
        admin.MapGet("/benchmarks/runs/{id}/calibrations", handlers.GetCalibrations);
    }
}
