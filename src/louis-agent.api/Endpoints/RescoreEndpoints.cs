using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Admin.AdminRows;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// <c>POST /v1/admin/benchmarks/runs/{id}/rescore</c> (F18 step 5b.2, ADR 0039: a verb on a resource): starts the eval runner on a stored run's export, which
/// grades its transcripts again with today's graders and records a new run that names this one. It asks no model, so it costs nothing. It starts, stores and
/// answers; the grading is the runner's. 201 with the new run's <c>Location</c>; 404 for a run the store doesn't hold; 400 for a run of a Python check;
/// 409 while a run runs; 503 without a store.
/// </summary>
internal static class RescoreEndpoints
{
    public static void MapRescore(this RouteGroupBuilder admin, BenchmarkCatalog catalog, BenchmarkRunner runner, IBenchmarkStore? store, BenchmarkRecorder? recorder,
        Func<BenchmarkCheck, IReadOnlyDictionary<string, string>> settingsOf)
    {
        admin.MapPost("/benchmarks/runs/{id}/rescore", (string id, CancellationToken cancellationToken) => AdminEndpoints.FromStore(store, async s =>
        {
            if (recorder is null)
                return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", MessageText.Format(ApiUserText.NoBenchmarkStore));
            if (await s.GetRunAsync(id, cancellationToken) is not { } stored)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));
            if (catalog.Checks.FirstOrDefault(c => c.Id == stored.Check) is not { } check)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error",
                    MessageText.Format(ApiUserText.RescoreNeedsEval, new { Id = id, Check = stored.Check }));
            if (runner.IsRunning)
                return Busy(runner);

            string export = await BenchmarkProtocolExport.BuildAsync(s, stored, cancellationToken);
            if (runner.StartRescore(check.Id, id, export, stored.Repeat) is not { } run) return Busy(runner);
            recorder.Attach(run, settingsOf(check));
            return Results.Created($"/v1/admin/benchmarks/runs/{run.Id}", Row(run));
        }));
    }

    private static IResult Busy(BenchmarkRunner runner) =>
        ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
}
