using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using StartAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.LiveRunRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using ReportsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.CalibrationsReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The judge checks of a stored run (F18 step 6a.5b): start one, which checks the judge against the people's grades
/// of the run, and list the reports kept, newest first. The runner judges; these only start it and show its report.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>AdminHandlers</c>: both routes
/// share the catalogue, the runner, the three stores and the recorder, which aren't services, so they live here and
/// each route is a named method a test can call with no HTTP. Each returns <c>TypedResults</c> with its answers
/// declared (<c>StartAnswer</c>, <c>ReportsAnswer</c>).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class CalibrationHandlers(
    BenchmarkCatalog catalog,
    BenchmarkRunner runner,
    IBenchmarkStore? store,
    IHumanReviewStore? reviews,
    ICalibrationStore? calibrations,
    CalibrationRecorder? recorder)
{
    /// <summary>
    /// Starts a judge check of a run against the people's grades: 202 with the run started; 400 for a grader nobody
    /// reviewed on that run, or a run of a script; 404 for a run the store doesn't hold, or one with no decided grades
    /// for that judge; 409 while a run runs; 503 without a store.
    /// </summary>
    public async Task<StartAnswer> StartCalibration(string id, string grader, CancellationToken cancellationToken)
    {
        if (store is null || reviews is null || calibrations is null || recorder is null) return NoStore();
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            var humans = ReviewQueries.HumanReviewsOf(run);
            if (!humans.Contains(grader + HumanReview.Suffix)) return NeedsAJudge(id, grader, humans);
            if (catalog.Checks.FirstOrDefault(k => k.Id == run.Check) is not { } check)
                return Bad(MessageText.Format(ReviewUserText.CalibrationNeedsEval, new
                {
                    Id = id,
                    Check = run.Check
                }));

            var labels = await ReviewQueries.DecidedLabelsAsync(store, reviews, id, grader, cancellationToken);
            if (labels.Count == 0)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
                    MessageText.Format(ReviewUserText.NoLabelsYet, new
                    {
                        Grader = grader,
                        Id = id
                    }));
            if (runner.IsRunning) return Busy();

            // One label per line, as the zip's files hold them: the runner reads them with the same loader.
            string lines = string.Concat(labels.Select(l => ReviewLabels.Line(l.Grade, l.Turns) + "\n"));
            if (runner.StartCalibration(check.Id, id, grader, lines) is not { } started) return Busy();
            recorder.Attach(started);
            return TypedResults.Json(LiveRunRow.Of(started), AdminJson.SnakeCase,
                statusCode: StatusCodes.Status202Accepted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>The run's judge-check reports, newest first; 404 for a run the store doesn't hold.</summary>
    public async Task<ReportsAnswer> GetCalibrations(string id, CancellationToken cancellationToken)
    {
        if (store is null || reviews is null || calibrations is null) return NoStore();
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is null) return NoRun(id);
            var reports = await calibrations.OfRunAsync(id, cancellationToken);
            return TypedResults.Json(new CalibrationsReply([.. reports.Select(CalibrationReportRow.Of)]),
                AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>The 400 for a grader nobody reviewed on that run, naming the ones that were.</summary>
    private static JsonHttpResult<ApiError> NeedsAJudge(string id, string grader, IReadOnlyCollection<string> humans)
    {
        string graders = string.Join(", ",
            humans.Select(h => h[..^HumanReview.Suffix.Length]).Order(StringComparer.Ordinal));
        return Bad(MessageText.Format(ReviewUserText.CalibrationNeedsAJudge,
            new
            {
                Grader = grader,
                Id = id,
                Graders = graders
            }));
    }

    /// <summary>The 503 for a store that isn't there.</summary>
    private static JsonHttpResult<ApiError> NoStore() =>
        ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
            MessageText.Format(ApiUserText.NoBenchmarkStore));

    /// <summary>The 503 for a store that didn't answer, saying why.</summary>
    private static JsonHttpResult<ApiError> Unavailable(Exception ex) =>
        ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", AdminEndpoints.Unavailable(ex));

    /// <summary>A 400 with its reason.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) =>
        ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", message);

    /// <summary>The 404 for a run the store doesn't hold.</summary>
    private static JsonHttpResult<ApiError> NoRun(string id) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
            MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));

    /// <summary>The 409 while another run is going.</summary>
    private JsonHttpResult<ApiError> Busy() =>
        ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error",
            MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
}
