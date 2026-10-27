namespace LouisAgent.Api.Endpoints.Workspace;

/// <summary>
/// Read-only browsing of the agent's workspace for the web app: a folder's entries and a file's text. The work is
/// <c>WorkspaceHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class WorkspaceEndpoints
{
    /// <summary>Adds the workspace routes.</summary>
    public static void MapWorkspace(this IEndpointRouteBuilder routes)
    {
        var handlers = new WorkspaceHandlers();

        routes.MapGet("/workspace/entries", handlers.GetEntries);
        routes.MapGet("/workspace/file", handlers.GetFile);
    }
}
