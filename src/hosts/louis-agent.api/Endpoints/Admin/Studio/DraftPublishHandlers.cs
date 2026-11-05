using LouisAgent.Composition.Messages;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Api.Evals.Studio.Publish;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using PublishAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Studio.PublishReply>,
    Microsoft.AspNetCore.Http.HttpResults.FileContentHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Studio;

/// <summary>
/// The compose page's Publish: the draft is written out as a folder, the runner must find no problem with it, and the
/// folder is committed beside its identity on <c>eval/{identity}-{name}</c> in the workspace, the push and the pull
/// request left to the person (finding S); a host with no git workspace hands the folder back as a zip.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), as the other studio handlers. Guard clauses (ADR 0051), in the order a person
/// would fix them: the draft, the runner, its verdict, the identity's project. Git stays the truth (ADR 0061): the
/// draft is kept after a publish, until the person deletes it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class DraftPublishHandlers(
    IEvalDraftStore drafts,
    DraftChecker checker,
    EvalHomes homes,
    string scratch)
{
    /// <summary>The workspace the eval is committed in.</summary>
    private string WorkspaceRoot => homes.WorkspaceRoot;

    /// <summary>Publishes the draft, or says why it can't be yet.</summary>
    public async Task<PublishAnswer> PostPublish(string id, CancellationToken cancellationToken)
    {
        try
        {
            if (await drafts.GetEvalAsync(id, cancellationToken) is not { } draft)
                return ApiErrors.NotFound(MessageText.Format(DraftMessage.NoEvalDraft, new { Id = id }));

            // What is published is what the runner checked: the folder it sealed and found no problem with.
            string folder = DraftFolder.Write(draft, await drafts.OfEvalAsync(draft.Name, cancellationToken), scratch,
                homes.Existing(draft));
            var verdict = checker.Check(folder);
            if (verdict.Failure is { } failure) return ApiErrors.Unavailable(failure);
            if (!verdict.Ok)
                return ApiErrors.BadRequest(MessageText.Format(DraftMessage.PublishNeedsAPassingCheck,
                    new
                    {
                        verdict.Problems.Count,
                        // The runner's sentence ends with its own full stop; the refusal adds the next one.
                        First = (verdict.Problems.FirstOrDefault() ?? "").TrimEnd('.')
                    }));

            // An edit goes back into the folder the runner lists it in (FX32); a new eval into a new one.
            var target = homes.TargetOf(draft);
            var publisher = new EvalPublisher(WorkspaceRoot);
            if (!publisher.CanCommit)
                return TypedResults.File(target.Zip(folder), "application/zip", $"{target.FolderName}.zip");
            if (!Directory.Exists(Path.Combine(WorkspaceRoot, target.IdentityFolder)))
                return ApiErrors.BadRequest(MessageText.Format(DraftMessage.PublishNoIdentity,
                    new { Folder = target.IdentityFolder }));

            var published = await publisher.PublishAsync(folder, target, MessageText.Format(
                DraftMessage.PublishCommitMessage, new
                {
                    draft.Name,
                    draft.Identity,
                    draft.Author
                }));
            return TypedResults.Json(new PublishReply(published.Branch, published.Commit, target.Folder),
                AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
        }
    }
}
