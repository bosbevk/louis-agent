using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Evals.Drafts;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Start;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using TryAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Runs.LiveRunRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Studio;

/// <summary>
/// The compose page's Try: the draft is written out as a folder and run on a few of its samples, replaying a stored
/// run, so it costs nothing (a question the recording doesn't have ends as a replay miss, never a model call). It is
/// recorded like any run, its settings saying it was a replay, so the run page shows its grades and the Benchmarks page
/// marks it (FX28).
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), as the run handlers: the route shares the draft store, the runner, the
/// recorder and the settings each run reports. Guard clauses (ADR 0051), one per refusal, before anything starts.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class DraftTryHandlers(
    IEvalDraftStore drafts,
    EvalProcess runner,
    EvalRecorder? recorder,
    Func<EvalEntry, IReadOnlyDictionary<string, string>> settingsOf,
    string scratch,
    string workspaceRoot)
{
    /// <summary>Writes the draft out and starts its try, answering 202 with the live run.</summary>
    public async Task<TryAnswer> PostTry(string id, TryRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Replay))
            return ApiErrors.BadRequest(MessageText.Format(DraftMessage.TryNeedsAReplay));
        var (options, refusal) = EvalRunOptions.Parse(null, request.Replay);
        if (refusal is not null) return ApiErrors.BadRequest(refusal);
        var samples = request.Samples ?? [];
        if (samples.FirstOrDefault(s => !KebabCase.Is(s)) is { } bad)
            return ApiErrors.BadRequest(MessageText.Format(DraftMessage.SampleIdNotKebabCase, new { Id = bad }));
        if (await drafts.GetEvalAsync(id, cancellationToken) is not { } draft)
            return ApiErrors.NotFound(MessageText.Format(DraftMessage.NoEvalDraft, new { Id = id }));
        if (recorder is null) return ApiErrors.Unavailable(MessageText.Format(ApiUserText.NoBenchmarkStore));
        if (runner.IsRunning) return Running();

        // The folder holds what will be published, and the runner runs it as a replay on the named samples.
        string folder = DraftFolder.Write(draft, await drafts.OfEvalAsync(draft.Name, cancellationToken), scratch,
            workspaceRoot);
        var eval = new EvalEntry(draft.Name, draft.Name);
        var tried = options! with
        {
            Folder = folder,
            Samples = samples.Count == 0 ? null : string.Join(",", samples)
        };
        if (runner.StartTry(eval, tried) is not { } run) return Running();

        recorder.Attach(run, EvalSettings.WithOptions(settingsOf(eval), run.Options));
        return TypedResults.Json(LiveRunRow.Of(run), AdminJson.SnakeCase, statusCode: StatusCodes.Status202Accepted);
    }

    /// <summary>The 409 when a run is already going: one run at a time.</summary>
    private Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<ApiError> Running() =>
        ApiErrors.Conflict(MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
}
