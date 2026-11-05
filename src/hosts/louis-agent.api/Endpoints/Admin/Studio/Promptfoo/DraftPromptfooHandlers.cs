using LouisAgent.Composition.Messages;
using System.Text;
using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Api.Evals.Studio.Publish;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using ImportAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.Api.Contracts.Admin.Studio.PromptfooImportReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using ExportAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Studio.Promptfoo;

/// <summary>
/// promptfoo on the studio's drafts: a promptfoo file is imported by the runner into a new eval draft, its file,
/// rubrics and scenarios kept and what has no counterpart named; a draft is exported by the runner as a promptfoo file,
/// so an eval made here can be run there.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), as the other studio handlers: the routes share the draft store, the runner's
/// promptfoo bridge, the clock and the scratch folder. Guard clauses (ADR 0051): the file, the runner, then the draft
/// rule in <see cref="ComposedEvals"/>, so an import is held to what a hand-made draft is.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class DraftPromptfooHandlers(
    IEvalDraftStore drafts,
    PromptfooRunner promptfoo,
    TimeProvider time,
    string scratch,
    EvalHomes homes)
{
    /// <summary>Makes a new eval draft from a promptfoo file.</summary>
    public async Task<ImportAnswer> PostImport(PromptfooImportRequest? request, CancellationToken cancellationToken)
    {
        if (request?.Yaml is not { Length: > 0 and <= ComposedEvals.MaxJsonLength } yaml)
            return ApiErrors.BadRequest(MessageText.Format(DraftMessage.PromptfooFileTooBig,
                new { Limit = ComposedEvals.MaxJsonLength }));

        try
        {
            var imported = promptfoo.Import(yaml, scratch);
            if (imported.Failure is { } failure) return ApiErrors.Unavailable(failure);
            var (made, refusal) = Make(imported, request.Author);
            if (made is not { } draft) return ApiErrors.BadRequest(refusal!);

            await drafts.AddAsync(draft, cancellationToken);
            return new CreatedJson<PromptfooImportReply>($"/v1/admin/evals/drafts/{draft.Id}",
                new PromptfooImportReply(EvalDraftRow.Of(draft), imported.LeftOut), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>The draft as a promptfoo file, its folder written out as a check would write it.</summary>
    public async Task<ExportAnswer> GetExport(string id, CancellationToken cancellationToken)
    {
        try
        {
            if (await drafts.GetEvalAsync(id, cancellationToken) is not { } draft)
                return ApiErrors.NotFound(MessageText.Format(DraftMessage.NoEvalDraft, new { Id = id }));

            string folder = DraftFolder.Write(draft, await drafts.OfEvalAsync(draft.Name, cancellationToken), scratch,
                homes.Existing(draft));
            var exported = promptfoo.Export(folder);
            if (exported.Failure is { } failure) return ApiErrors.Unavailable(failure);
            return TypedResults.Text(exported.Stdout, "application/yaml", Encoding.UTF8);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>The draft for what the runner imported, or the words <see cref="ComposedEvals"/> refused it in.</summary>
    private (EvalDraft? Draft, string? Refusal) Make(PromptfooImported imported, string? author)
    {
        try
        {
            return (ComposedEvals.Of(ShortId.New(), imported.Json, imported.Rubrics, author, time.GetUtcNow(),
                imported.Scenarios), null);
        }
        catch (ArgumentException ex)
        {
            return (null, ex.Message);
        }
    }
}
