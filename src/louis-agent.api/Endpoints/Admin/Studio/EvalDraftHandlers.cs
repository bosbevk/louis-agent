using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Endpoints.Answers;
using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using MadeAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    LouisAgent.Api.Endpoints.Answers.CreatedJson<LouisAgent.Api.Contracts.Admin.Studio.EvalDraftRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using DraftAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Studio.EvalDraftRow>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using ListAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Studio.EvalDraftsReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using DeleteAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.NoContent,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Studio;

/// <summary>
/// The compose page's drafts: an eval being composed is made, replaced, read, listed and deleted, its name and
/// identity read from its file. A draft is the studio's waiting room: it changes no eval until it is published.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>DraftHandlers</c>: the routes
/// share the draft store and the clock, so each is a named method a test calls with no HTTP. What a draft may hold is
/// <see cref="ComposedEvals"/>' rule; its refusals come back as a 400 in their own words.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
internal sealed class EvalDraftHandlers(IEvalDraftStore drafts, TimeProvider time)
{
    /// <summary>Where the drafts are, below the API's root.</summary>
    private const string Path = "/v1/admin/evals/drafts";

    /// <summary>Keeps a new eval being composed.</summary>
    public async Task<MadeAnswer> PostDraft(EvalDraftRequest? request, CancellationToken cancellationToken)
    {
        var (made, refusal) = Make(ShortId.New(), request);
        if (made is not { } draft) return Bad(refusal!);
        try
        {
            await drafts.AddAsync(draft, cancellationToken);
            return new CreatedJson<EvalDraftRow>($"{Path}/{draft.Id}", EvalDraftRow.Of(draft), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>Replaces an eval being composed with what the page sent now.</summary>
    public async Task<DraftAnswer> PutDraft(string id, EvalDraftRequest? request, CancellationToken cancellationToken)
    {
        var (made, refusal) = Make(id, request);
        if (made is not { } draft) return Bad(refusal!);
        try
        {
            if (!await drafts.ReplaceAsync(draft, cancellationToken)) return NoDraft(id);
            return TypedResults.Json(EvalDraftRow.Of(draft), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>One eval being composed.</summary>
    public async Task<DraftAnswer> GetDraft(string id, CancellationToken cancellationToken)
    {
        try
        {
            if (await drafts.GetEvalAsync(id, cancellationToken) is not { } draft) return NoDraft(id);
            return TypedResults.Json(EvalDraftRow.Of(draft), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>Every eval being composed, oldest first.</summary>
    public async Task<ListAnswer> GetDrafts(CancellationToken cancellationToken)
    {
        try
        {
            var all = await drafts.EvalsAsync(cancellationToken);
            return TypedResults.Json(new EvalDraftsReply([.. all.Select(EvalDraftRow.Of)]), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>Deletes an eval being composed; an id of another kind of draft is not one.</summary>
    public async Task<DeleteAnswer> DeleteDraft(string id, CancellationToken cancellationToken)
    {
        try
        {
            if (await drafts.GetEvalAsync(id, cancellationToken) is null) return NoDraft(id);
            await drafts.DeleteAsync(id, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>The draft for the request, or the words <see cref="ComposedEvals"/> refused it with.</summary>
    private (EvalDraft? Draft, string? Refusal) Make(string id, EvalDraftRequest? request)
    {
        try
        {
            return (ComposedEvals.Of(id, request?.Json, request?.Rubrics.ToFiles(), request?.Author,
                time.GetUtcNow(), request?.Scenarios.ToFiles()), null);
        }
        catch (ArgumentException ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>A 400 that says what was wrong.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) => ApiErrors.BadRequest(message);

    /// <summary>The 404 for an eval draft that isn't there.</summary>
    private static JsonHttpResult<ApiError> NoDraft(string id) =>
        ApiErrors.NotFound(MessageText.Format(DraftMessage.NoEvalDraft, new { Id = id }));

    /// <summary>The 503 when the store didn't answer, with its reason.</summary>
    private static JsonHttpResult<ApiError> Unavailable(Exception ex) =>
        ApiErrors.Unavailable(AdminEndpoints.Unavailable(ex));
}
