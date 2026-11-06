namespace LouisAgent.Api.Endpoints.Admin.Reports;

/// <summary>Hangs the reports' route on the admin group, so the admin key covers it (F35.7).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API's routes under v1</seealso>
internal static class ReportEndpoints
{
    /// <summary>Adds <c>GET /reports</c>.</summary>
    public static void MapReports(this RouteGroupBuilder admin) =>
        admin.MapGet("/reports", ReportHandlers.GetReports);
}
