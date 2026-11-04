using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Api.Evals.Studio.Publish;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using CheckAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Studio.DraftCheckReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Studio;

/// <summary>
/// The compose page's Check: the draft is written out as a folder, with the eval's sample and transcript drafts, and
/// the eval runner judges it as a run would; the page gets whether it conforms and every problem in the runner's words.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), as <c>EvalDraftHandlers</c>: the route shares the draft store, the runner and
/// the scratch folder the drafts are written under and the workspace an edited eval starts from, so it is a named
/// method a test calls with a stand-in runner.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class DraftCheckHandlers(
    IEvalDraftStore drafts,
    DraftChecker checker,
    string scratch,
    EvalHomes homes)
{
    /// <summary>Writes the draft out and has the runner judge it.</summary>
    public async Task<CheckAnswer> PostVerdict(string id, CancellationToken cancellationToken)
    {
        try
        {
            if (await drafts.GetEvalAsync(id, cancellationToken) is not { } draft)
                return ApiErrors.NotFound(MessageText.Format(DraftMessage.NoEvalDraft, new { Id = id }));

            // The folder holds what will be published: the draft and the samples and transcripts kept for its eval.
            string folder = DraftFolder.Write(draft, await drafts.OfEvalAsync(draft.Name, cancellationToken), scratch,
                homes.Existing(draft));
            var check = checker.Check(folder);
            if (check.Failure is { } failure) return ApiErrors.Unavailable(failure);
            return TypedResults.Json(new DraftCheckReply(check.Ok, check.Problems), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
        }
    }
}
