using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Studio;

namespace LouisAgent.Api.Endpoints.Admin.Studio;

/// <summary>
/// Hangs the studio's routes on the admin group, so the admin key covers them: the catalogue of checks and readers,
/// the evals being composed, and the runner's check of one. The work is in <c>StudioHandlers</c>,
/// <c>EvalDraftHandlers</c> and <c>DraftCheckHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class StudioEndpoints
{
    /// <summary>
    /// The folder the drafts are written out under to be checked, tried and published: the machine's temp folder.
    /// </summary>
    internal static readonly string Scratch = Path.Combine(Path.GetTempPath(), "louis-agent-studio");

    /// <summary>Adds <c>GET /evals/checks</c>, the <c>/evals/drafts</c> resource and a draft's check.</summary>
    public static void MapStudio(this RouteGroupBuilder admin, CheckList checks, IEvalDraftStore drafts,
        TimeProvider time, DraftChecker checker)
    {
        var checking = new DraftCheckHandlers(drafts, checker, Scratch);
        admin.MapPost("/evals/drafts/{id}/verdict", checking.PostVerdict);

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
