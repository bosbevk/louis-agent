namespace LouisAgent.Api.Endpoints.Admin.Imports;

/// <summary>
/// Hangs the uploads' routes on the admin group, so the admin key covers them (F55): the list, and each upload's
/// preview and save.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class ImportEndpoints
{
    /// <summary>Adds <c>GET /imports</c>, <c>POST /imports/{name}/preview</c> and <c>POST /imports/{name}/save</c>.</summary>
    public static void MapImports(this RouteGroupBuilder admin)
    {
        admin.MapGet("/imports", ImportHandlers.GetImports);
        admin.MapPost("/imports/{name}/preview", ImportHandlers.PostPreview);
        admin.MapPost("/imports/{name}/save", ImportHandlers.PostSave);
    }
}
