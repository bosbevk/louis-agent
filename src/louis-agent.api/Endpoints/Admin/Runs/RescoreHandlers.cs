using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Evals.Export;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using RescoreAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.Api.Contracts.Admin.Runs.LiveRunRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// Grades a stored run's transcripts again with today's graders. The eval runner reads the run's export and records a
/// new run that points back at this one. No model is asked, so it costs nothing.
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
    EvalList catalog,
    EvalProcess runner,
    IEvalStore? store,
    EvalRecorder? recorder,
    Func<EvalEntry, IReadOnlyDictionary<string, string>> settingsOf)
{
    /// <summary>
    /// Starts the re-score and points you at the new run. Only a stored run of an eval can be re-scored, and not while
    /// another run is going.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), one per refusal, before the start.</remarks>
    public async Task<RescoreAnswer> Rescore(string id, CancellationToken cancellationToken)
    {
        if (store is null || recorder is null)
            return ApiErrors.Unavailable(MessageText.Format(ApiUserText.NoBenchmarkStore));
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } stored)
                return ApiErrors.NotFound(MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));
            if (catalog.Checks.FirstOrDefault(c => c.Id == stored.Check) is not { } check)
                return ApiErrors.BadRequest(MessageText.Format(ApiUserText.RescoreNeedsEval,
                    new
                    {
                        Id = id,
                        Check = stored.Check
                    }));
            if (runner.IsRunning) return Busy();

            // The runner grades the run's own transcript, so hand it the export; a start that loses a race is a 409.
            string export = await EvalProtocolExport.BuildAsync(store, stored, cancellationToken);
            if (runner.StartRescore(check.Id, id, export, stored.Repeat) is not { } run) return Busy();

            recorder.Attach(run, settingsOf(check));
            return new CreatedJson<LiveRunRow>($"/v1/admin/benchmarks/runs/{run.Id}", LiveRunRow.Of(run),
                AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>The 409 while another run is going, naming its check.</summary>
    private JsonHttpResult<ApiError> Busy() =>
        ApiErrors.Conflict(MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
}
