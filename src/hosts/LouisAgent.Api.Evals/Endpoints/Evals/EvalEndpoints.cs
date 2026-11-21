using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Evals.Endpoints.Evals;

/// <summary>The admin's eval routes, inside the admin group, so the admin key guards them like the rest.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class EvalEndpoints
{
    /// <summary>Adds the eval list to the admin group.</summary>
    public static void MapEvals(this RouteGroupBuilder admin, EvalList catalog, IEvalStore? store,
        string workspaceRoot, IEvalDraftStore drafts)
    {
        var handlers = new EvalHandlers(catalog, store, workspaceRoot, drafts);
        admin.MapGet("/evals", handlers.GetList);
    }
}
