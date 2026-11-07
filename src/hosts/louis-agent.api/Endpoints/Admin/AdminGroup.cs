using LouisAgent.Api.Endpoints.Routing;

namespace LouisAgent.Api.Endpoints.Admin;

/// <summary>
/// The <c>/admin</c> group behind the admin key, for the API's own admin routes and for whatever a library maps on
/// it (the eval studio, CL57): each caller gets its own group with the same gate, so the key check runs before any
/// route's own work and the route table says which calls take the admin key.
/// </summary>
public static class AdminGroup
{
    /// <summary>A gated <c>/admin</c> group to map routes on.</summary>
    public static RouteGroupBuilder MapAdminGroup(this IEndpointRouteBuilder routes, string? adminKey) =>
        routes.MapGroup("/admin").WithAccess(RouteAccess.AdminKey)
            .AddEndpointFilter(new AdminGateFilter(adminKey).Gate);
}
