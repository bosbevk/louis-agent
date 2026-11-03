using LouisAgent.Api.Evals.Studio;

namespace LouisAgent.Api.Endpoints.Admin.Studio;

/// <summary>
/// Hangs the studio's routes on the admin group, so the admin key covers them; the work is in
/// <c>StudioHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class StudioEndpoints
{
    /// <summary>Adds the catalogue route: <c>GET /evals/checks</c>.</summary>
    public static void MapStudio(this RouteGroupBuilder admin, CheckList checks)
    {
        var handlers = new StudioHandlers(checks);
        admin.MapGet("/evals/checks", handlers.GetChecks);
    }
}
