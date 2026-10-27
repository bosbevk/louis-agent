using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Admin.AdminRows;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The judge checks of a stored run (F18 step 6a.5b, ADR 0039: a resource, <c>calibrations</c>, under the run). <c>POST …/calibrations/{grader}</c> checks the judge against the people's
/// grades of the run: the decided grades become label lines on the eval runner's stdin, and the runner's <c>--calibrate</c> asks the judge about each; 202 with the run started
/// (<c>calibration_of</c>, <c>grader</c>); 400 for a grader without a human review on that run, or a run of a script; 404 for a run the store doesn't hold, or one nobody has graded for that
/// judge; 409 while a run runs; 503 without a store. <c>GET …/calibrations</c> lists the reports kept, newest first. The runner judges; the API starts it, keeps its report and shows it.
/// They belong to the same <c>/admin</c> group as the rest, so the key gate covers them.
/// </summary>
internal static class CalibrationEndpoints
{
    public static void MapCalibrations(this RouteGroupBuilder admin, BenchmarkCatalog catalog, BenchmarkRunner runner, IBenchmarkStore? store, IHumanReviewStore? reviews,
        ICalibrationStore? calibrations, CalibrationRecorder? recorder)
    {
        admin.MapPost("/benchmarks/runs/{id}/calibrations/{grader}", (string id, string grader, CancellationToken cancellationToken) =>
            Guarded(store, reviews, calibrations, async (s, r, c) =>
            {
                if (recorder is null) return NoStore();
                if (await s.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
                var humans = ReviewEndpoints.HumanReviewsOf(run);
                if (!humans.Contains(grader + HumanReview.Suffix))
                    return Bad(MessageText.Format(ReviewUserText.CalibrationNeedsAJudge, new { Grader = grader, Id = id, Graders = string.Join(", ", humans.Select(h => h[..^HumanReview.Suffix.Length]).Order(StringComparer.Ordinal)) }));
                if (catalog.Checks.FirstOrDefault(k => k.Id == run.Check) is not { Kind: BenchmarkCheckKind.Eval } check)
                    return Bad(MessageText.Format(ReviewUserText.CalibrationNeedsEval, new { Id = id, Check = run.Check }));
                var labels = await ReviewEndpoints.DecidedLabelsAsync(s, r, id, grader, cancellationToken);
                if (labels.Count == 0)
                    return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ReviewUserText.NoLabelsYet, new { Grader = grader, Id = id }));
                if (runner.IsRunning) return Busy(runner);

                // One label per line, as the zip's files hold them: the runner reads them with the same loader.
                string lines = string.Concat(labels.Select(l => ReviewLabels.Line(l.Grade, l.Turns) + "\n"));
                if (runner.StartCalibration(check.Id, id, grader, lines) is not { } started) return Busy(runner);
                recorder.Attach(started);
                return Results.Json(Row(started), statusCode: StatusCodes.Status202Accepted);
            }));

        admin.MapGet("/benchmarks/runs/{id}/calibrations", (string id, CancellationToken cancellationToken) => Guarded(store, reviews, calibrations, async (s, _, c) =>
        {
            if (await s.GetRunAsync(id, cancellationToken) is null) return NoRun(id);
            return Results.Json(new { calibrations = (await c.OfRunAsync(id, cancellationToken)).Select(CalibrationRow) });
        }));
    }

    private static async Task<IResult> Guarded(IBenchmarkStore? store, IHumanReviewStore? reviews, ICalibrationStore? calibrations,
        Func<IBenchmarkStore, IHumanReviewStore, ICalibrationStore, Task<IResult>> work)
    {
        if (store is null || reviews is null || calibrations is null) return NoStore();
        try
        {
            return await work(store, reviews, calibrations);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", AdminEndpoints.Unavailable(ex));
        }
    }

    private static IResult NoStore() => ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", MessageText.Format(ApiUserText.NoBenchmarkStore));

    private static IResult Bad(string message) => ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", message);

    private static IResult NoRun(string id) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));

    private static IResult Busy(BenchmarkRunner runner) =>
        ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
}
