using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Evals.Endpoints.Drafts;

/// <summary>
/// Hangs the studio's draft routes: a sample and a transcript made from a stored run, the scenario a sample starts
/// from, and an eval's drafts listed and deleted. They sit in the admin group, so the key covers them; the work is in
/// <c>DraftHandlers</c> and <c>DraftListHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class DraftEndpoints
{
    /// <summary>Adds the draft routes to the group the eval routes hang on.</summary>
    public static void MapDrafts(this RouteGroupBuilder evals, EvalList catalog, IEvalStore? store,
        IHumanReviewStore? reviews, IEvalDraftStore drafts, string workspaceRoot, TimeProvider time)
    {
        var made = new DraftHandlers(catalog, store, reviews, drafts, workspaceRoot, time);
        evals.MapPost("/runs/{id}/samples", made.PostSample);
        evals.MapPost("/runs/{id}/transcripts", made.PostTranscript);
        evals.MapGet("/runs/{id}/scenarios/{task}", made.GetScenario);

        var listed = new DraftListHandlers(catalog, drafts);
        evals.MapGet("/{eval}/drafts", listed.GetDrafts);
        evals.MapDelete("/{eval}/drafts/{draft}", listed.DeleteDraft);
    }
}
