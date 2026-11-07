namespace LouisAgent.Api.Endpoints.Admin.Screens;

/// <summary>Hangs the screens' routes on the admin group, so the admin key covers them.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: API routes under /v1</seealso>
internal static class ScreenEndpoints
{
    /// <summary>Adds the list, a screen's rows, a row as a form, and asking it to act.</summary>
    public static void MapScreens(this RouteGroupBuilder admin)
    {
        admin.MapGet("/screens", ScreenHandlers.GetScreens);
        admin.MapGet("/screens/{name}/rows", ScreenHandlers.GetRows);
        admin.MapGet("/screens/{name}/rows/{id}", ScreenRowHandlers.GetRow);
        admin.MapPut("/screens/{name}/rows/{id}", ScreenRowHandlers.PutRow);
        admin.MapPost("/screens/{name}/actions/{action}", ScreenActionHandlers.PostAction);
    }
}
