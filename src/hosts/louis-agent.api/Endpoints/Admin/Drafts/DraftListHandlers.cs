using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Drafts;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using DraftsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Drafts.DraftsReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using DeleteAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.NoContent,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Drafts;

/// <summary>
/// An eval's drafts on the Evals page: list them, samples and transcripts, and delete one. A draft is deleted only
/// under its own eval, so a link from one eval's card can't remove another's.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>DraftHandlers</c>: the routes share
/// the catalogue and the draft store. Split from <c>DraftHandlers</c> by what changes them: these read and remove
/// drafts by eval, those make them from a run.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class DraftListHandlers(EvalList catalog, IEvalDraftStore drafts)
{
    /// <summary>Every draft of the eval, oldest first.</summary>
    public async Task<DraftsAnswer> GetDrafts(string eval, CancellationToken cancellationToken)
    {
        if (catalog.Checks.All(c => c.Id != eval)) return NoEval(eval);
        try
        {
            return TypedResults.Json(DraftsReply.Of(await drafts.OfEvalAsync(eval, cancellationToken)),
                AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>Deletes one of the eval's drafts, whichever kind it is.</summary>
    public async Task<DeleteAnswer> DeleteDraft(string eval, string draft, CancellationToken cancellationToken)
    {
        try
        {
            // The draft must be this eval's: a sample or a transcript under that name.
            var sample = await drafts.GetSampleAsync(draft, cancellationToken);
            var transcript = await drafts.GetTranscriptAsync(draft, cancellationToken);
            if (sample?.Eval != eval && transcript?.Eval != eval) return NoDraft(eval, draft);

            await drafts.DeleteAsync(draft, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>The 404 for an eval the runner's catalogue doesn't have.</summary>
    private static JsonHttpResult<ApiError> NoEval(string eval) =>
        ApiErrors.NotFound(MessageText.Format(DraftMessage.NoEval, new { Eval = eval }));

    /// <summary>The 404 for a draft the eval doesn't have.</summary>
    private static JsonHttpResult<ApiError> NoDraft(string eval, string draft) =>
        ApiErrors.NotFound(MessageText.Format(DraftMessage.NoDraft,
            new
            {
                Eval = eval,
                Id = draft
            }));
}
