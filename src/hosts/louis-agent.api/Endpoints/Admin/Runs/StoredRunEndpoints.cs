using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;

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
    public static void MapStoredRuns(this RouteGroupBuilder evals, IEvalStore? store, EvalProcess runner,
        IHumanReviewStore? reviews = null,
        ICalibrationStore? calibrations = null)
    {
        var handlers = new StoredRunHandlers(store, runner, reviews, calibrations);

        evals.MapGet("/runs/{id}", handlers.GetRun);
        evals.MapGet("/runs/{id}/turns/{mode}/{repeat:int}/{task}/{turn:int}/events", handlers.GetTurnEvents);
        evals.MapGet("/runs/{id}/protocol", handlers.GetProtocol);
        evals.MapDelete("/runs/{id}", handlers.DeleteRun);
    }
}
