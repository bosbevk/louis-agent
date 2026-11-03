using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Studio;

namespace LouisAgent.Api.Endpoints.Admin.Studio;

/// <summary>
/// Hangs the studio's routes on the admin group, so the admin key covers them: the catalogue of checks and readers,
/// and the evals being composed. The work is in <c>StudioHandlers</c> and <c>EvalDraftHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class StudioEndpoints
{
    /// <summary>Adds <c>GET /evals/checks</c> and the <c>/evals/drafts</c> resource.</summary>
    public static void MapStudio(this RouteGroupBuilder admin, CheckList checks, IEvalDraftStore drafts,
        TimeProvider time)
    {
        var catalogue = new StudioHandlers(checks);
        admin.MapGet("/evals/checks", catalogue.GetChecks);

        var composed = new EvalDraftHandlers(drafts, time);
        admin.MapPost("/evals/drafts", composed.PostDraft);
        admin.MapGet("/evals/drafts", composed.GetDrafts);
        admin.MapGet("/evals/drafts/{id}", composed.GetDraft);
        admin.MapPut("/evals/drafts/{id}", composed.PutDraft);
        admin.MapDelete("/evals/drafts/{id}", composed.DeleteDraft);
    }
}
