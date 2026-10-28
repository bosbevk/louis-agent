using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using RescoreAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.Api.Contracts.Admin.Runs.LiveRunRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// Grades a stored run's transcripts again with today's graders (F18 step 5b.2): the eval runner reads the run's
/// export and records a new run that names this one. It asks no model, so it costs nothing; the grading is the
/// runner's.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>AdminHandlers</c>: the route needs
/// the catalogue, the runner, the store, the recorder and the run settings, which aren't services, so they live here
/// and the route is a named method. It answers <c>CreatedJson</c>, a 201 written with the admin wire's options (the
/// unattended run's finding 2).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class RescoreHandlers(
    BenchmarkCatalog catalog,
    BenchmarkRunner runner,
    IBenchmarkStore? store,
    BenchmarkRecorder? recorder,
    Func<BenchmarkCheck, IReadOnlyDictionary<string, string>> settingsOf)
{
    /// <summary>
    /// Starts the re-score: 201 with the new run at its <c>Location</c>; 404 for a run the store doesn't hold; 400 for
    /// a run of a check that isn't an eval; 409 while a run runs; 503 without a store.
    /// </summary>
    public async Task<RescoreAnswer> Rescore(string id, CancellationToken cancellationToken)
    {
        if (store is null || recorder is null)
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
                MessageText.Format(ApiUserText.NoBenchmarkStore));
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } stored)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
                    MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));
            if (catalog.Checks.FirstOrDefault(c => c.Id == stored.Check) is not { } check)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error",
                    MessageText.Format(ApiUserText.RescoreNeedsEval,
                        new
                        {
                            Id = id,
                            Check = stored.Check
                        }));
            if (runner.IsRunning) return Busy();

            string export = await BenchmarkProtocolExport.BuildAsync(store, stored, cancellationToken);
            if (runner.StartRescore(check.Id, id, export, stored.Repeat) is not { } run) return Busy();
            recorder.Attach(run, settingsOf(check));
            return new CreatedJson<LiveRunRow>($"/v1/admin/benchmarks/runs/{run.Id}", LiveRunRow.Of(run),
                AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
                AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>The 409 while another run is going.</summary>
    private JsonHttpResult<ApiError> Busy() =>
        ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error",
            MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
}
