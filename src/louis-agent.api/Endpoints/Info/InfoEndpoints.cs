using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Api.Endpoints.Info;

/// <summary>
/// What this instance is and has: its health, the price table and exchange rates it loaded (public, for other hosts
/// and the web app), the toolsets a session can load, the model, the roles and the LLM profiles. The work is
/// <c>InfoHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class InfoEndpoints
{
    /// <summary>
    /// The probe, the one route outside <c>/v1</c>: a load balancer or a compose health check calls it without a key.
    /// </summary>
    public static void MapHealth(this IEndpointRouteBuilder routes) =>
        routes.MapGet("/health", InfoHandlers.GetHealth);

    /// <summary>Adds the routes that describe this instance.</summary>
    public static void MapInfo(this IEndpointRouteBuilder routes, AgentHostContext host)
    {
        var handlers = new InfoHandlers(host);

        // Public like /health: prices, rates and the display currency aren't secret, and another host fetches them
        // without the API key.
        routes.MapGet("/prices", handlers.GetPrices).WithAccess(RouteAccess.Open);
        routes.MapGet("/rates", handlers.GetRates).WithAccess(RouteAccess.Open);
        routes.MapGet("/rates/display", handlers.GetDisplay).WithAccess(RouteAccess.Open);

        routes.MapGet("/toolsets", handlers.GetToolsets);
        routes.MapGet("/model", handlers.GetModel);
        routes.MapGet("/roles", handlers.GetRoles);
        routes.MapGet("/llm-profiles", handlers.GetLlmProfiles);
    }
}
