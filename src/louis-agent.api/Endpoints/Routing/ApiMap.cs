using LouisAgent.Core.Guardrails.Workspace;
using LouisAgent.Core.Hosting;
using LouisAgent.Api.Sessions;

namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>
/// The API in one place (CL20, ADR 0039): the services its endpoints take and the calls that map them. <c>Program.cs</c> and the
/// route tests both build the API through it, so the table the tests see is the one that runs, and a route added anywhere else
/// fails a test. The admin portal's parts (F27, F31) are built by the host and mapped on what <see cref="MapApi"/> returns.
/// </summary>
internal static class ApiMap
{
    /// <summary>The services the endpoints take, from the host the agent was built as.</summary>
    public static IServiceCollection AddApi(this IServiceCollection services, AgentHostContext host)
    {
        // host.Engine (every toolset this instance can load) serves /workspace and /git; sessions get their own (F5).
        services.AddSingleton(host.Engine);
        services.AddSingleton(host.Engines);
        services.AddSingleton<AgentSessions>();
        services.AddSingleton(new WorkspaceBoundary(host.Engine.WorkspaceRoot));
        return services;
    }

    /// <summary>Maps every route of the agent's API and returns what the admin portal maps its routes on.</summary>
    public static IEndpointRouteBuilder MapApi(this WebApplication app, AgentHostContext host)
    {
        app.MapInfo(host);
        app.MapWorkspace();
        app.MapGit();
        app.MapHistory();
        app.MapSessions(host);
        return app;
    }
}
