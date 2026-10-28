using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using StartAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Runs.LiveRunRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using ReportsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Reviews.CalibrationsReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Reviews;

/// <summary>
/// Starts a judge check of a stored run, which measures the judge against people's grades of the run, and lists the
/// reports kept, newest first. The runner does the judging; this only starts it and shows what it found.
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
    EvalList catalog,
    EvalProcess runner,
    IEvalStore? store,
    IHumanReviewStore? reviews,
    ICalibrationStore? calibrations,
    CalibrationRecorder? recorder)
{
    /// <summary>
    /// Starts checking a judge against people's grades of a run. It needs a stored run of an eval, a judge people
    /// reviewed on it with at least one decided grade, and no other run going.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), one per refusal, before the start.</remarks>
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
            // A start that loses the race to another run is a 409 too.
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

    /// <summary>Lists the judge-check reports kept for a run, newest first.</summary>
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

    /// <summary>The 400 for a judge nobody reviewed on that run, listing the ones people did review.</summary>
    private static JsonHttpResult<ApiError> NeedsAJudge(string id, string grader, IReadOnlyCollection<string> humans)
    {
        // The judges' own names: each human review without its suffix, in a fixed order.
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

    /// <summary>The 503 when one of the stores isn't there.</summary>
    private static JsonHttpResult<ApiError> NoStore() =>
        ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
            MessageText.Format(ApiUserText.NoBenchmarkStore));

    /// <summary>The 503 when the store didn't answer, with its reason.</summary>
    private static JsonHttpResult<ApiError> Unavailable(Exception ex) =>
        ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", AdminEndpoints.Unavailable(ex));

    /// <summary>A 400 that says what was wrong.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) =>
        ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", message);

    /// <summary>The 404 for a run the store doesn't hold.</summary>
    private static JsonHttpResult<ApiError> NoRun(string id) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
            MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));

    /// <summary>The 409 while another run is going, naming its check.</summary>
    private JsonHttpResult<ApiError> Busy() =>
        ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error",
            MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
}
