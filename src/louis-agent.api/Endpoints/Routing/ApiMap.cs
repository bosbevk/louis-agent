using LouisAgent.Api.Endpoints.Git;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions;
using LouisAgent.Core.Guardrails.Workspace;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>
/// The API in one place (CL20, ADR 0039): the services its endpoints take and the calls that map them. <c>Program.cs</c> and the
/// route tests both build the API through it, so the table the tests see is the one that runs, and a route mapped in
/// <c>Program.cs</c> fails a test. Every route but <c>/health</c> is under <see cref="Version"/>, the one place the version is
/// written. The admin portal's parts (F27, F31) are built by the host and mapped on what <see cref="MapApi"/> returns.
/// </summary>
internal static class ApiMap
{
    /// <summary>The version every route but the probe lives under; a breaking change adds <c>/v2</c> beside it.</summary>
    public const string Version = "/v1";

    /// <summary>The services the endpoints take, from the host the agent was built as.</summary>
    public static IServiceCollection AddApi(this IServiceCollection services, AgentHostContext host)
    {
        // host.Engine (every toolset this instance can load) serves /v1/workspace and /v1/git; sessions get their own (F5).
        services.AddSingleton(host.Engine);
        services.AddSingleton(host.Engines);
        services.AddSingleton<AgentSessions>();
        services.AddSingleton(new WorkspaceBoundary(host.Engine.WorkspaceRoot));
        return services;
    }

    /// <summary>Maps every route of the agent's API and returns the <c>/v1</c> group the admin portal maps its routes on.</summary>
    public static IEndpointRouteBuilder MapApi(this WebApplication app, AgentHostContext host)
    {
        app.MapHealth();

        var v1 = app.MapGroup(Version);
        v1.MapInfo(host);
        v1.MapWorkspace();
        v1.MapGitChanges();
        v1.MapGitCommits();
        v1.MapGitBranches();
        v1.MapSessions(host);

        // A /v1 path no route matches is the API's JSON 404, not the web app's page (which the fallback outside /v1 would serve).
        v1.MapFallback("{**path}", (string path) =>
            ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.UnknownRoute, new { Path = path })));
        return v1;
    }
}
