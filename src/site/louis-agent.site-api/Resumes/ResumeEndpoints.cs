using LouisAgent.Api.Endpoints.Admin;
using LouisAgent.Api.Endpoints.Routing;

namespace LouisAgent.SiteApi.Resumes;

/// <summary>
/// Maps the CV's routes on the API's <c>/v1</c> group (F142): <c>GET /v1/site/cv</c> for anyone, since the CV is a
/// public page, and <c>PUT /v1/admin/cv</c> behind the admin key, for the admin's editor.
/// </summary>
/// <remarks>
/// The site's own routes, marked and gated as the API's are (Louis's pick of three: open the API's seam), so the key
/// gate and the admin gate treat them like any other route.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
public static class ResumeEndpoints
{
    /// <summary>The CV's two routes, on the group <c>MapLouisAgentHostGroup</c> hands out.</summary>
    public static IEndpointRouteBuilder MapSiteResume(this IEndpointRouteBuilder v1, ResumeStore store,
        string? adminKey)
    {
        var handlers = new ResumeHandlers(store);
        v1.MapGet("/site/cv", handlers.Get).WithAccess(RouteAccess.Open);

        // The editor reads its copy here and not on the open route, so it can't open without the admin's key (FX153).
        var admin = v1.MapAdminGroup(adminKey);
        admin.MapGet("/cv", handlers.Get);
        admin.MapPut("/cv", handlers.PutAsync);
        return v1;
    }
}
