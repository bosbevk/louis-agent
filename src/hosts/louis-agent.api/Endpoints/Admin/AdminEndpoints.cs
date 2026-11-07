using LouisAgent.Api.Endpoints.Admin.Imports;
using LouisAgent.Api.Endpoints.Admin.Prompts;
using LouisAgent.Api.Endpoints.Admin.Screens;

namespace LouisAgent.Api.Endpoints.Admin;

/// <summary>
/// The API's own admin routes, behind the key: every built-in prompt as its header says it (CL27), the data a person
/// uploads into the packs (F55), and the screens the packs describe (ADR 0086). The eval studio's routes are
/// louis-agent.api.evals's, mapped on the same kind of group (CL57).
/// </summary>
internal static class AdminEndpoints
{
    /// <summary>Adds the admin routes under <c>/admin</c>, behind the key.</summary>
    public static void MapAdmin(this IEndpointRouteBuilder routes, string? adminKey)
    {
        var admin = routes.MapAdminGroup(adminKey);
        admin.MapGet("/prompts", PromptsHandlers.GetPrompts);
        admin.MapImports();
        admin.MapScreens();
    }
}
